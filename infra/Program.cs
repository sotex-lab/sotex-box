using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Pulumi.Aws.Ecs;
using Pulumi.Aws.Ecs.Inputs;
using Pulumi.Aws.Iam;
using Pulumi.Aws.LB;
using Pulumi.Aws.LB.Inputs;

class SotexBoxStack : Stack
{
    public SotexBoxStack()
    {
        var vpc = new Vpc(
            "backend-vpc",
            new VpcArgs
            {
                CidrBlock = "10.0.0.0/16",
                EnableDnsHostnames = true,
                EnableDnsSupport = true
            }
        );

        var networkAcl = new NetworkAcl(
            "backend-acl",
            new NetworkAclArgs
            {
                VpcId = vpc.Id,
                Egress =
                {
                    new NetworkAclEgressArgs
                    {
                        RuleNo = 100,
                        Action = "allow",
                        Protocol = "-1",
                        FromPort = 0,
                        ToPort = 0,
                        CidrBlock = "0.0.0.0/0"
                    }
                }
            }
        );

        var internetGateway = new InternetGateway(
            "backend-internet-gateway",
            new InternetGatewayArgs { VpcId = vpc.Id }
        );

        var routeTable = new RouteTable(
            "backend-route-table",
            new RouteTableArgs { VpcId = vpc.Id }
        );

        new Route(
            "backend-default-route",
            new RouteArgs
            {
                RouteTableId = routeTable.Id,
                DestinationCidrBlock = "0.0.0.0/0",
                GatewayId = internetGateway.Id
            }
        );

        var firstSubnet = new Subnet(
            "backend-subnet-1a",
            new SubnetArgs
            {
                VpcId = vpc.Id,
                CidrBlock = "10.0.3.0/24",
                AvailabilityZone = "eu-north-1a"
            }
        );

        var secondSubnet = new Subnet(
            "backend-subnet-1b",
            new SubnetArgs
            {
                VpcId = vpc.Id,
                CidrBlock = "10.0.2.0/24",
                AvailabilityZone = "eu-north-1b"
            }
        );
        var securityGroup = new SecurityGroup(
            "backend-sg",
            new SecurityGroupArgs
            {
                VpcId = vpc.Id,
                Egress = new InputList<SecurityGroupEgressArgs>
                {
                    new SecurityGroupEgressArgs
                    {
                        Protocol = "-1", // All protocols
                        FromPort = 0,
                        ToPort = 0,
                        CidrBlocks = { "0.0.0.0/0" }
                    }
                }
            }
        );

        var taskExecutionRole = new Role(
            "backend-task-execution-role",
            new RoleArgs
            {
                AssumeRolePolicy =
                    @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [{
                    ""Action"": ""sts:AssumeRole"",
                    ""Effect"": ""Allow"",
                    ""Principal"": {
                        ""Service"": ""ecs-tasks.amazonaws.com""
                    }
                }]
            }"
            }
        );

        new RolePolicyAttachment(
            "backend-task-role-policy-attach",
            new RolePolicyAttachmentArgs
            {
                Role = taskExecutionRole.Name,
                PolicyArn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
            }
        );

        var appTaskDefinition = new TaskDefinition(
            "backend-task-definition",
            new TaskDefinitionArgs
            {
                Family = "backend-task-family",
                Cpu = "1024",
                Memory = "4096",
                NetworkMode = "awsvpc",
                RequiresCompatibilities = { "FARGATE" },
                ExecutionRoleArn = taskExecutionRole.Arn,
                ContainerDefinitions =
                    @"[
                {
                    ""name"": ""backend"",
                    ""image"": ""nginx"",
                    ""portMappings"": [{
                        ""containerPort"": 80,
                        ""hostPort"": 80
                    }]
                }
            ]"
            }
        );

        var cluster = new Cluster("backend-cluster", ClusterArgs.Empty);

        var loadBalancer = new LoadBalancer(
            "backend-load-balancer",
            new LoadBalancerArgs
            {
                Subnets = { firstSubnet.Id, secondSubnet.Id },
                SecurityGroups = { securityGroup.Id }
            }
        );

        var targetGroup = new TargetGroup(
            "backend-target-group",
            new TargetGroupArgs
            {
                VpcId = vpc.Id,
                Port = 80,
                Protocol = "HTTP",
                TargetType = "ip"
            }
        );

        var listener = new Listener(
            "backend-listener",
            new ListenerArgs
            {
                LoadBalancerArn = loadBalancer.Arn,
                Protocol = "HTTP",
                Port = 80,
                DefaultActions = new List<ListenerDefaultActionArgs>
                {
                    new ListenerDefaultActionArgs
                    {
                        Type = "forward",
                        TargetGroupArn = targetGroup.Arn
                    }
                }
            }
        );

        var appService = new Service(
            "backend-service",
            new ServiceArgs
            {
                Cluster = cluster.Arn,
                DesiredCount = 1,
                LaunchType = "FARGATE",
                TaskDefinition = appTaskDefinition.Arn,
                NetworkConfiguration = new ServiceNetworkConfigurationArgs
                {
                    AssignPublicIp = true,
                    Subnets = { firstSubnet.Id, secondSubnet.Id },
                    SecurityGroups = { securityGroup.Id }
                },
                LoadBalancers = new InputList<ServiceLoadBalancerArgs>
                {
                    new ServiceLoadBalancerArgs
                    {
                        ContainerName = "backend",
                        ContainerPort = 80,
                        TargetGroupArn = targetGroup.Arn
                    }
                }
            },
            new CustomResourceOptions { DependsOn = { listener } }
        );

        ServiceName = appService.Name;
        ClusterName = cluster.Name;
        BackendUrl = Output.Format($"http://{loadBalancer.DnsName}");
    }

    [Output]
    public Output<string> ServiceName { get; set; }

    [Output]
    public Output<string> ClusterName { get; set; }

    [Output]
    public Output<string> BackendUrl { get; set; }
}

class Program
{
    static Task<int> Main(string[] args) => Deployment.RunAsync<SotexBoxStack>();
}
