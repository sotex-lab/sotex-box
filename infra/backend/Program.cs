using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigTranslator;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Pulumi.Aws.Rds;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;
using Pulumi.Aws.Sqs;

class SotexBoxStack : Stack
{
    public SotexBoxStack()
    {
        var mapped = new PulumiMapper(new Config()).Map<MappedConfig>();
        var vpc = new Vpc(
            "vpc",
            new VpcArgs
            {
                CidrBlock = mapped.NetworkingVpcCidrBlock,
                EnableDnsSupport = true,
                EnableDnsHostnames = true
            }
        );

        var igw = new InternetGateway("gateway", new InternetGatewayArgs { VpcId = vpc.Id });

        var routeTable = new RouteTable(
            "routingTable",
            new RouteTableArgs
            {
                VpcId = vpc.Id,
                Routes =
                {
                    new RouteTableRouteArgs { CidrBlock = "0.0.0.0/0", GatewayId = igw.Id, }
                },
            }
        );

        var subnets = new List<Subnet>();
        for (int i = 0; i < mapped.NetworkingSubnetsCidrBlock!.Count(); i++)
        {
            var subnet = new Subnet(
                string.Format("subnet-{0}", i),
                new SubnetArgs
                {
                    VpcId = vpc.Id,
                    CidrBlock = mapped.NetworkingSubnetsCidrBlock!.ElementAt(i),
                    AvailabilityZone = mapped.NetworkingSubnetsAvailabilityZone!.ElementAt(i),
                    MapPublicIpOnLaunch = true
                }
            );

            var routeTableAssociation = new RouteTableAssociation(
                string.Format("rta-{0}", i),
                new RouteTableAssociationArgs { RouteTableId = routeTable.Id, SubnetId = subnet.Id }
            );

            subnets.Add(subnet);
        }

        var subnetGroup = new SubnetGroup(
            "subnet-group",
            new SubnetGroupArgs
            {
                Name = "subnet-group",
                SubnetIds = subnets.Select(x => x.Id).ToList()
            }
        );

        var buckets = new Dictionary<string, Bucket>();
        foreach (var bucket in mapped.Buckets!)
        {
            var bucketItem = new Bucket(bucket, BucketArgs.Empty);
            buckets[bucket] = bucketItem;
        }

        var queue = new Queue(mapped.SqsProcessorQueue, QueueArgs.Empty);

        var queuePolicy = new QueuePolicy(
            "queuePolicy",
            new QueuePolicyArgs
            {
                QueueUrl = queue.Id,
                Policy = Output.Format(
                    $@"{{
                ""Version"": ""2012-10-17"",
                ""Statement"": [{{
                    ""Effect"": ""Allow"",
                    ""Principal"": {{""Service"": ""s3.amazonaws.com""}},
                    ""Action"": ""SQS:SendMessage"",
                    ""Resource"": ""{queue.Arn}"",
                    ""Condition"": {{
                        ""ArnEquals"": {{ ""aws:SourceArn"": ""{buckets["non-processed"].Arn}"" }}
                    }}
                }}]
            }}"
                )
            }
        );

        var bucketNotification = new BucketNotification(
            "addedToNonProcessed",
            new BucketNotificationArgs
            {
                Bucket = buckets["non-processed"].Id,
                Queues = new List<BucketNotificationQueueArgs>
                {
                    new BucketNotificationQueueArgs
                    {
                        Events = new List<string> { "s3:ObjectCreated:*" },
                        QueueArn = queue.Arn
                    }
                }
            },
            new CustomResourceOptions { DependsOn = { queuePolicy } }
        );

        var dbSecGroup = new SecurityGroup(
            "db-sec-group",
            new SecurityGroupArgs
            {
                Description = "allow all inbound trafic",
                VpcId = vpc.Id,
                Ingress =
                {
                    new SecurityGroupIngressArgs
                    {
                        Protocol = "-1",
                        FromPort = 0,
                        ToPort = 0,
                        CidrBlocks = { "0.0.0.0/0" }
                    }
                }
            }
        );

        var access_key = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY")!;
        var secret_key = Environment.GetEnvironmentVariable("AWS_SECRET_KEY")!;
        var db_password = Convert.ToBase64String(Encoding.UTF8.GetBytes(secret_key));

        var dbInstance = new Pulumi.Aws.Rds.Instance(
            "postgres",
            new Pulumi.Aws.Rds.InstanceArgs
            {
                AllocatedStorage = mapped.DbAllocatedStorage,
                Engine = mapped.DbEngine,
                EngineVersion = mapped.DbEngineVersion,
                InstanceClass = mapped.DbInstanceClass,
                DbName = mapped.DbEngine,
                Password = db_password,
                Username = "sotex",
                VpcSecurityGroupIds = dbSecGroup.Id,
                DbSubnetGroupName = subnetGroup.Name,
                SkipFinalSnapshot = true,
            }
        );

        var connectionString = Output
            .Tuple(dbInstance.Endpoint, dbInstance.DbName, dbInstance.Username, dbInstance.Password)
            .Apply(t =>
            {
                (string endpoint, string name, string username, string? password) = t;
                return $"Host={endpoint};Database={name};Username={username};Password={password}";
            });

        // Create a new security group for HTTP and SSH access
        var securityGroup = new SecurityGroup(
            "web-sg",
            new SecurityGroupArgs
            {
                Description = "Enable HTTP and SSH access",
                VpcId = vpc.Id,
                Ingress =
                {
                    new SecurityGroupIngressArgs
                    {
                        Protocol = "tcp",
                        FromPort = 22,
                        ToPort = 22,
                        CidrBlocks = { "0.0.0.0/0" },
                    },
                    new SecurityGroupIngressArgs
                    {
                        Protocol = "tcp",
                        FromPort = 80,
                        ToPort = 80,
                        CidrBlocks = { "0.0.0.0/0" },
                    }
                },
                Egress =
                {
                    new SecurityGroupEgressArgs
                    {
                        Protocol = "-1",
                        FromPort = 0,
                        ToPort = 0,
                        CidrBlocks = { "0.0.0.0/0" }
                    }
                }
            }
        );

        // Find an Ubuntu AMI to use
        var ami = GetAmi.Invoke(
            new GetAmiInvokeArgs
            {
                Filters =
                {
                    new GetAmiFilterInputArgs { Name = "name", Values = { mapped.UbuntuAmi }, }
                },
                Owners = { "099720109477" }, // Canonical
                MostRecent = true
            }
        );

        var keyPair = new KeyPair(
            "ec2KeyPair",
            new KeyPairArgs { KeyName = "key-pair", PublicKey = mapped.PublicKey }
        );

        var envVariables = new Dictionary<string, Output<string>>
        {
            ["GF_SECURITY_ADMIN_PASSWORD"] = Output.Create(secret_key),
            ["NGINX_PORT"] = Output.Create("80"),
            ["CONNECTION_STRING"] = connectionString,
            ["AWS_S3_URL"] = Output.Create(mapped.S3Url),
            ["AWS_REGION"] = Output.Create("eu-central-1"),
            ["AWS_S3_ACCESS_KEY"] = Output.Create(access_key),
            ["AWS_S3_SECRET_KEY"] = Output.Create(secret_key),
            ["AWS_SQS_URL"] = Output.Create(mapped.SqsUrl),
            ["AWS_SQS_ACCESS_KEY"] = Output.Create(access_key),
            ["AWS_SQS_SECRET_KEY"] = Output.Create(secret_key),
            ["AWS_SQS_NONPROCESSED_QUEUE_URL"] = queue.Url.Apply(url =>
                url.Split(mapped.SqsUrl)[1]
            ),
            ["REQUIRE_KNOWN_DEVICES"] = Output.Create("true"),
            ["NOOP_CRON"] = Output.Create("0/15 * * ? * *"),
            ["SQS_CRON"] = Output.Create("0/15 * * ? * *"),
            ["SCHEDULE_MAX_DELAY"] = Output.Create("00:30:00"),
            ["SCHEDULE_DEVICE_THRESHOLD"] = Output.Create("100"),
            ["CALCULATE_AD_THRESHOLD"] = Output.Create("10"),
            ["CALCULATE_URL_EXPIRE"] = Output.Create("10:00:00")
        };

        foreach (var bucket in buckets)
        {
            envVariables[string.Format("{0}_BUCKET_NAME", bucket.Key.ToUpper().Replace('-', '_'))] =
                bucket.Value.Id;
        }

        var envFile = Output.Create("");
        foreach (var pair in envVariables)
        {
            var key = pair.Key;
            envFile = Output
                .Tuple(envFile, pair.Value)
                .Apply(t => $"{t.Item1}\n{key}=\"{t.Item2}\"");
        }

        // Spin up a new EC2 instance
        var instance = new Pulumi.Aws.Ec2.Instance(
            "web-instance",
            new Pulumi.Aws.Ec2.InstanceArgs
            {
                InstanceType = mapped.Ec2InstanceType,
                Ami = ami.Apply(a => a.Id),
                KeyName = keyPair.KeyName,
                VpcSecurityGroupIds = { securityGroup.Id },
                SubnetId = subnets[0].Id,
                UserData = envFile.Apply(x =>
                    @$"#!/bin/bash
                    set -euo pipefail

                    cd /home/ubuntu
                    echo 'export COMPOSE_COMMAND=docker-compose' >> .bashrc
                    echo 'export ANDROID_HOME=notImportant' >> .bashrc
                    sudo apt -y update
                    sudo apt install -y git docker.io make gzip
                    sudo systemctl start docker
                    sudo docker run hello-world
                    sudo systemctl enable docker
                    sudo usermod -a -G docker ubuntu

                    sudo curl -L https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose
                    sudo chmod +x /usr/local/bin/docker-compose

                    newgrp docker

                    curl -L -O https://github.com/tamasfe/taplo/releases/download/0.8.0/taplo-full-linux-x86_64.gz
                    gzip -d taplo-full-linux-x86_64.gz
                    chmod +x taplo-full-linux-x86_64
                    sudo mv taplo-full-linux-x86_64 /usr/local/bin/taplo

                    git clone https://github.com/sotex-lab/sotex-box.git
                    cat <<EOF > sotex-box/.env
{x}
EOF
                    sudo chown -R ubuntu:ubuntu sotex-box

                    ip=$(dig +short myip.opendns.com @resolver1.opendns.com | sed s/\\./-/g)
                    echo DOMAIN_NAME=ec2-$ip.eu-central-1.compute.amazonaws.com >> sotex-box/.env
                    "
                ),
            }
        );

        PublicDns = instance.PublicDns;
        PublicIp = instance.PublicIp.Apply(ip => $"ubuntu@{ip}");
    }

    [Output]
    public Output<string> PublicDns { get; set; }

    [Output]
    public Output<string> PublicIp { get; set; }
}

class Program
{
    static Task<int> Main() => Deployment.RunAsync<SotexBoxStack>();
}
