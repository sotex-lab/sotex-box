using System.Collections.Generic;
using System.Text.Json;
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
        var stackName = Deployment.Instance.StackName;
        var config = new Config();

        var vpc = new Vpc(
            string.Format("{0}-vpc", stackName),
            new VpcArgs { CidrBlock = config.Require("networking-vpc-cidrBlock"), }
        );

        var internetGateway = new InternetGateway(
            string.Format("{0}-internet-gateway", stackName),
            new InternetGatewayArgs { VpcId = vpc.Id }
        );

        var publicRouteTable = new RouteTable(
            string.Format("{0}-route-table", stackName),
            new RouteTableArgs
            {
                Routes =
                {
                    new RouteTableRouteArgs
                    {
                        CidrBlock = config.Require("networking-publicRoutingTable-cidrBlock"),
                        GatewayId = internetGateway.Id,
                    }
                },
                VpcId = vpc.Id,
            }
        );

        var firstSubnet = new Subnet(
            string.Format("{0}-subnet-1a", stackName),
            new SubnetArgs
            {
                VpcId = vpc.Id,
                CidrBlock = config.RequireObject<List<string>>("networking-subnets-cidrBlock")[0],
                AvailabilityZone = config.RequireObject<List<string>>(
                    "networking-subnets-availabilityZone"
                )[0],
            }
        );

        new RouteTableAssociation(
            string.Format("{0}-subnet-1a-assosiation", stackName),
            new RouteTableAssociationArgs
            {
                SubnetId = firstSubnet.Id,
                RouteTableId = publicRouteTable.Id
            }
        );

        var secondSubnet = new Subnet(
            string.Format("{0}-subnet-1b", stackName),
            new SubnetArgs
            {
                VpcId = vpc.Id,
                CidrBlock = config.RequireObject<List<string>>("networking-subnets-cidrBlock")[1],
                AvailabilityZone = config.RequireObject<List<string>>(
                    "networking-subnets-availabilityZone"
                )[1],
            }
        );

        new RouteTableAssociation(
            string.Format("{0}-subnet-1b-assosiation", stackName),
            new RouteTableAssociationArgs
            {
                SubnetId = secondSubnet.Id,
                RouteTableId = publicRouteTable.Id
            }
        );

        var securityGroup = new SecurityGroup(
            string.Format("{0}-security-group", stackName),
            new SecurityGroupArgs
            {
                VpcId = vpc.Id,
                Egress =
                {
                    new SecurityGroupEgressArgs
                    {
                        Protocol = config.Require("networking-securityGroup-egress-protocol"),
                        FromPort = config.RequireInt32("networking-securityGroup-egress-fromPort"),
                        ToPort = config.RequireInt32("networking-securityGroup-egress-toPort"),
                        CidrBlocks =
                        {
                            config.Require("networking-securityGroup-egress-cidrBlocks")
                        }
                    }
                },
                Ingress =
                {
                    new SecurityGroupIngressArgs
                    {
                        Protocol = config.Require("networking-securityGroup-ingress-protocol"),
                        FromPort = config.RequireInt32("networking-securityGroup-ingress-fromPort"),
                        ToPort = config.RequireInt32("networking-securityGroup-ingress-toPort"),
                        CidrBlocks =
                        {
                            config.Require("networking-securityGroup-ingress-cidrBlocks")
                        }
                    }
                }
            }
        );

        var cluster = new Cluster(string.Format("{0}-cluster", stackName));
        var rolePolicy = JsonSerializer.Serialize(
            new
            {
                Version = config.Require("rolePolicy-version"),
                Statement = new[]
                {
                    new
                    {
                        Sid = config.Require("rolePolicy-statement-sid"),
                        Effect = config.Require("rolePolicy-statement-effect"),
                        Principal = new
                        {
                            Service = config.Require("rolePolicy-statement-principalService")
                        },
                        Action = config.Require("rolePolicy-statement-action")
                    }
                }
            }
        );

        var taskExecRole = new Role(
            string.Format("{0}-task-execution-role", stackName),
            new RoleArgs { AssumeRolePolicy = rolePolicy }
        );

        new RolePolicyAttachment(
            string.Format("{0}-task-exec-policy", stackName),
            new RolePolicyAttachmentArgs
            {
                Role = taskExecRole.Name,
                PolicyArn = config.Require("rolePolicy-policyArn")
            }
        );

        var loadBalancer = new LoadBalancer(
            string.Format("{0}-load-balancer", stackName),
            new LoadBalancerArgs
            {
                Subnets = { firstSubnet.Id, secondSubnet.Id },
                SecurityGroups = { securityGroup.Id },
            }
        );

        var targetGroup = new TargetGroup(
            string.Format("{0}-target-group", stackName),
            new TargetGroupArgs
            {
                Port = config.RequireInt32("networking-targetGroup-port"),
                Protocol = config.Require("networking-targetGroup-protocol"),
                TargetType = config.Require("networking-targetGroup-targetType"),
                VpcId = vpc.Id,
            }
        );

        var listener = new Listener(
            string.Format("{0}-listener", stackName),
            new ListenerArgs
            {
                LoadBalancerArn = loadBalancer.Arn,
                Port = config.RequireInt32("networking-listener-port"),
                DefaultActions =
                {
                    new ListenerDefaultActionArgs
                    {
                        Type = config.Require("networking-listener-actionType"),
                        TargetGroupArn = targetGroup.Arn
                    }
                }
            }
        );

        var taskDefinition = new TaskDefinition(
            string.Format("{0}-task", stackName),
            new TaskDefinitionArgs
            {
                Family = "fargate-task-definition",
                Cpu = config.Require("taskDefinition-cpu"),
                Memory = config.Require("taskDefinition-memory"),
                NetworkMode = "awsvpc",
                RequiresCompatibilities = { "FARGATE" },
                ExecutionRoleArn = taskExecRole.Arn,
                ContainerDefinitions = JsonSerializer.Serialize(
                    new[]
                    {
                        new
                        {
                            name = stackName,
                            image = config.Require("taskDefinition-backend-image"),
                            portMappings = new[]
                            {
                                new
                                {
                                    containerPort = config.RequireInt32(
                                        "taskDefinition-backend-containerPort"
                                    ),
                                    hostPort = config.RequireInt32(
                                        "taskDefinition-backend-hostPort"
                                    ),
                                    protocol = config.Require("taskDefinition-backend-protocol")
                                }
                            }
                        }
                    }
                )
            }
        );

        new Service(
            string.Format("{0}-service", stackName),
            new ServiceArgs
            {
                Cluster = cluster.Arn,
                DesiredCount = config.RequireInt32("service-desiredCount"),
                LaunchType = "FARGATE",
                TaskDefinition = taskDefinition.Arn,
                NetworkConfiguration = new ServiceNetworkConfigurationArgs
                {
                    AssignPublicIp = true,
                    Subnets = { firstSubnet.Id, secondSubnet.Id },
                    SecurityGroups = { securityGroup.Id }
                },
                LoadBalancers =
                {
                    new ServiceLoadBalancerArgs
                    {
                        TargetGroupArn = targetGroup.Arn,
                        ContainerName = stackName,
                        ContainerPort = config.RequireInt32("taskDefinition-backend-containerPort")
                    }
                }
            },
            new CustomResourceOptions { DependsOn = listener }
        );

        BackendUrl = Output.Format($"http://{loadBalancer.DnsName}");
    }

    [Output]
    public Output<string> BackendUrl { get; set; }
}

class Program
{
    static Task<int> Main() => Deployment.RunAsync<SotexBoxStack>();
}
