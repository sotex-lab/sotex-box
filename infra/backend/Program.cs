using System.Threading.Tasks;
using Pulumi;
using Pulumi.Aws;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Rds;
using Pulumi.Aws.S3;
using Pulumi.Aws.Sqs;

class SotexBoxStack : Stack
{
    public SotexBoxStack()
    {
        var vpc = new Vpc(
            "vpc",
            new VpcArgs
            {
                CidrBlock = "10.0.0.0/16",
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

        var subnet = new Subnet(
            "subnet",
            new SubnetArgs
            {
                VpcId = vpc.Id,
                CidrBlock = "10.0.1.0/24",
                MapPublicIpOnLaunch = true,
                AvailabilityZone = "eu-central-1a"
            }
        );
        var subnet2 = new Subnet(
            "subnet2",
            new SubnetArgs
            {
                VpcId = vpc.Id,
                CidrBlock = "10.0.2.0/24",
                MapPublicIpOnLaunch = true,
                AvailabilityZone = "eu-central-1b"
            }
        );

        var subnetGroup = new SubnetGroup(
            "subnet-group",
            new SubnetGroupArgs { Name = "subnet-group", SubnetIds = { subnet.Id, subnet2.Id } }
        );

        var routeTableAssociation = new RouteTableAssociation(
            "rta",
            new RouteTableAssociationArgs { RouteTableId = routeTable.Id, SubnetId = subnet.Id }
        );

        var processed = new Bucket("processed", BucketArgs.Empty);
        var nonprocessed = new Bucket("non-processed", BucketArgs.Empty);
        var schedule = new Bucket("schedule", BucketArgs.Empty);
        S3Url = Output.Format($"https://s3.eu-central-1.amazonaws.com");
        ProcessedBucketName = processed.BucketName.Apply(name => name);
        NonProcessedBucketName = nonprocessed.BucketName.Apply(name => name);
        ScheduleBucketName = schedule.BucketName.Apply(name => name);

        var queue = new Queue("queue", QueueArgs.Empty);
        SqsUrl = queue.Url.Apply(url => url);

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

        var dbInstance = new Pulumi.Aws.Rds.Instance(
            "postgres",
            new Pulumi.Aws.Rds.InstanceArgs
            {
                AllocatedStorage = 5,
                Engine = "postgres",
                EngineVersion = "16.2",
                InstanceClass = "db.t3.micro",
                DbName = "postgres",
                Password = "sotex123",
                Username = "sotex",
                VpcSecurityGroupIds = dbSecGroup.Id,
                DbSubnetGroupName = subnetGroup.Name,
                SkipFinalSnapshot = true,
            }
        );

        ConnectionString = Output
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
                    new GetAmiFilterInputArgs
                    {
                        Name = "name",
                        Values = { "ubuntu/images/hvm-ssd/ubuntu-focal-20.04-amd64-server-*" },
                    }
                },
                Owners = { "099720109477" }, // Canonical
                MostRecent = true
            }
        );

        var keyPair = new KeyPair(
            "ec2KeyPair",
            new KeyPairArgs
            {
                KeyName = "key-pair",
                PublicKey =
                    "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIJ8HeJ+7gg3jHS6b25feqENPpul4qBqwm07eS7oOxfuF"
            }
        );

        // Spin up a new EC2 instance
        var instance = new Pulumi.Aws.Ec2.Instance(
            "web-instance",
            new Pulumi.Aws.Ec2.InstanceArgs
            {
                InstanceType = "t2.micro",
                Ami = ami.Apply(a => a.Id),
                KeyName = keyPair.KeyName,
                VpcSecurityGroupIds = { securityGroup.Id },
                SubnetId = subnet.Id,
                UserData =
                    @"#!/bin/bash
                    set -euo pipefail

                    cd /home/ubuntu
                    echo 'export COMPOSE_COMMAND=docker-compose' >> .bashrc
                    sudo apt -y update
                    sudo apt install -y git docker.io make
                    sudo systemctl start docker
                    sudo docker run hello-world
                    sudo systemctl enable docker
                    sudo usermod -a -G docker ubuntu

                    sudo curl -L https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose
                    sudo chmod +x /usr/local/bin/docker-compose

                    newgrp docker
                    git clone https://github.com/sotex-lab/sotex-box.git
                    sudo chown -R ubuntu:ubuntu sotex-box",
            }
        );
        PublicDns = instance.PublicDns;
        PublicIp = instance.PublicIp;
    }

    [Output]
    public Output<string> PublicDns { get; set; }

    [Output]
    public Output<string> PublicIp { get; set; }

    [Output]
    public Output<string> S3Url { get; set; }

    [Output]
    public Output<string> S3Region { get; set; }

    [Output]
    public Output<string> SqsUrl { get; set; }

    [Output]
    public Output<string> ConnectionString { get; set; }

    [Output]
    public Output<string> ProcessedBucketName { get; set; }

    [Output]
    public Output<string> NonProcessedBucketName { get; set; }

    [Output]
    public Output<string> ScheduleBucketName { get; set; }
}

class Program
{
    static Task<int> Main() => Deployment.RunAsync<SotexBoxStack>();
}
