using System.Threading.Tasks;
using Pulumi;
using Pulumi.Aws;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Pulumi.Aws.Iam;
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
                MapPublicIpOnLaunch = true
            }
        );

        var routeTableAssociation = new RouteTableAssociation(
            "rta",
            new RouteTableAssociationArgs { RouteTableId = routeTable.Id, SubnetId = subnet.Id }
        );

        var bucket = new Bucket("bucket", BucketArgs.Empty);
        S3Url = Output.Format($"s3://{bucket.BucketName}");
        S3Region = Output.Create(GetRegion.InvokeAsync()).Apply(region => region.Name);

        var queue = new Queue("queue", QueueArgs.Empty);
        SqsUrl = queue.Id.Apply(id =>
            $"https://sqs.{Output.Create(GetRegion.InvokeAsync()).Apply(region => region.Name)}.amazonaws.com/<accountId>/{id}"
        );

        var dbSecGroup = new SecurityGroup(
            "db-sec-group",
            new SecurityGroupArgs
            {
                Description = "allow all inbound trafic",
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
                SkipFinalSnapshot = true
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
                    "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQDJ78GLgD+3djgVse41XinimL5jO1h0FrFCXU7hm1+tXnLS1av6uV6sQKGYqYYDCs+b0aZhp1hqt6WRjbLkc20JMZi9ZlVtHgtY9IO396dzljFEj8VZ141TnNvwAmzD2sWuPGN/chI4/u8b16E/znwY4xXNFH2ydfUloYiNAcRI7pYv1ixuGWqu7OKSKZf9P9qTqoVJQisku9Gt5wkKNBnWuNkiX8hWeQeyR/0LsX1MIG5/ncuf9e/N16EzXB9yWi2o+DNpk9Yv36RDkygcuv4WMimVqJp5WXqLenWGntVkBCK8Yb8YwjdFHi5uUX0BFam+aJ9z8VqXALM3GbhMQXPxRr1ZMeJO+uD6tNTvbvoXz3uTqwt9BnI4nQJmdREzODf+JhuFTq7DBtLfq7v+TnsERkX8rJ4aONwOqW/07t3pr13NdSYjfwnvxXm2M/i2u6dYFvDJOBdaIq8Cs7tBkpdqe+eKTt1l0rL9HPJWSLpnZuYwvEjtJ93MvGnBZbVfDaH88jypqARiTAZd8poBO5EoHv8JSaWpYyRfdOd1Z/XBPSKhFcItIg8DZaFbjqG8NS3Zhc/RvxpVWFCTcrKW8kKofp821nHkQoX62yfEgzKbJyJMEqBor1Xjqr3aNz5x2m2vr2A8M842ID6Ec6nVqtyUMo7NtNqpfV/pt81ubTBA3Q=="
            }
        );

        // Spin up a new EC2 instance
        var instance = new Instance(
            "web-instance",
            new InstanceArgs
            {
                InstanceType = "t2.micro",
                Ami = ami.Apply(a => a.Id),
                KeyName = keyPair.KeyName,
                VpcSecurityGroupIds = { securityGroup.Id },
                SubnetId = subnet.Id,
                UserData =
                    @"#!/bin/bash
                    set -euo pipefail

                    sudo apt -y update
                    sudo apt install -y git docker.io make
                    sudo systemctl start docker
                    sudo docker run hello-world
                    sudo systemctl enable docker
                    sudo usermod -a -G docker ubuntu

                    sudo curl -L https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose
                    sudo chmod +x /usr/local/bin/docker-compose

                    newgrp docker
                    git clone https://github.com/sotex-lab/sotex-box.git",
            }
        );
        PublicDns = instance.PublicDns;
        PublicIp = instance.PublicIp;
    }

    [Output]
    public Output<string> PublicDns { get; set; }
    public Output<string> PublicIp { get; set; }
    public Output<string> S3Url { get; set; }
    public Output<string> S3Region { get; set; }
    public Output<string> SqsUrl { get; set; }
    public Output<string> ConnectionString { get; set; }
}

class Program
{
    static Task<int> Main() => Deployment.RunAsync<SotexBoxStack>();
}
