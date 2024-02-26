using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigTranslator;
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
        var mapped = new PulumiMapper(config).Map<MappedConfig>();

        var vpc = new Vpc(
            string.Format("{0}-vpc", stackName),
            new VpcArgs { CidrBlock = mapped.NetworkingVpcCidrBlock, }
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
                        CidrBlock = mapped.NetworkingPublicRoutingTableCidrBlock,
                        GatewayId = internetGateway.Id,
                    }
                },
                VpcId = vpc.Id,
            }
        );

        var subnets = new List<Subnet>();
        for (int i = 0; i < mapped.NetworkingSubnetsCidrBlock!.Count(); i++)
        {
            var subnet = new Subnet(
                string.Format("{0}-subnet-{1}", stackName, i),
                new SubnetArgs
                {
                    VpcId = vpc.Id,
                    CidrBlock = mapped.NetworkingSubnetsCidrBlock!.ElementAt(i),
                    AvailabilityZone = mapped.NetworkingSubnetsAvailabilityZone!.ElementAt(i),
                }
            );

            new RouteTableAssociation(
                string.Format("{0}-subnet-{1}-assosiation", stackName, i),
                new RouteTableAssociationArgs
                {
                    SubnetId = subnet.Id,
                    RouteTableId = publicRouteTable.Id
                }
            );

            subnets.Add(subnet);
        }

        var securityGroup = new SecurityGroup(
            string.Format("{0}-security-group", stackName),
            new SecurityGroupArgs
            {
                VpcId = vpc.Id,
                Egress =
                {
                    new SecurityGroupEgressArgs
                    {
                        Protocol = mapped.NetworkingSecurityGroupEgressProtocol,
                        FromPort = mapped.NetworkingSecurityGroupEgressFromPort,
                        ToPort = mapped.NetworkingSecurityGroupEgressToPort,
                        CidrBlocks = { mapped.NetworkingSecurityGroupEgressCidrBlocks }
                    }
                },
                Ingress =
                {
                    new SecurityGroupIngressArgs
                    {
                        Protocol = mapped.NetworkingSecurityGroupIngressProtocol,
                        FromPort = mapped.NetworkingSecurityGroupIngressFromPort,
                        ToPort = mapped.NetworkingSecurityGroupIngressToPort,
                        CidrBlocks = { mapped.NetworkingSecurityGroupIngressCidrBlocks }
                    }
                }
            }
        );

        var cluster = new Cluster(string.Format("{0}-cluster", stackName));
        var rolePolicy = JsonSerializer.Serialize(
            new
            {
                Version = mapped.RolePolicyVersion,
                Statement = new[]
                {
                    new
                    {
                        Sid = mapped.RolePolicyStatementSid,
                        Effect = mapped.RolePolicyStatementEffect,
                        Principal = new { Service = mapped.RolePolicyStatementPrincipalService },
                        Action = mapped.RolePolicyStatementAction
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
                PolicyArn = mapped.RolePolicyPolicyArn
            }
        );

        var loadBalancer = new LoadBalancer(
            string.Format("{0}-load-balancer", stackName),
            new LoadBalancerArgs
            {
                Subnets = subnets.Select(x => x.Id).ToList(),
                SecurityGroups = { securityGroup.Id },
            }
        );

        var targetGroup = new TargetGroup(
            string.Format("{0}-target-group", stackName),
            new TargetGroupArgs
            {
                Port = mapped.NetworkingTargetGroupPort,
                Protocol = mapped.NetworkingTargetGroupProtocol,
                TargetType = mapped.NetworkingTargetGroupTargetType,
                VpcId = vpc.Id,
            }
        );

        var listener = new Listener(
            string.Format("{0}-listener", stackName),
            new ListenerArgs
            {
                LoadBalancerArn = loadBalancer.Arn,
                Port = mapped.NetworkingListenerPort,
                DefaultActions =
                {
                    new ListenerDefaultActionArgs
                    {
                        Type = mapped.NetworkingListenerActionType,
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
                Cpu = mapped.TaskDefinitionCpu,
                Memory = mapped.TaskDefinitionMemory,
                NetworkMode = "awsvpc",
                RequiresCompatibilities = { "FARGATE" },
                ExecutionRoleArn = taskExecRole.Arn,
                ContainerDefinitions = JsonSerializer.Serialize(
                    new[]
                    {
                        new
                        {
                            name = stackName,
                            image = mapped.TaskDefintionBackendImage,
                            portMappings = new[]
                            {
                                new
                                {
                                    containerPort = mapped.TaskDefinitionBackendContainerPort,
                                    hostPort = mapped.TaskDefinitionBackendHostPort,
                                    protocol = mapped.TaskDefinitionBackendProtocol
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
                DesiredCount = mapped.ServiceDesiredCount,
                LaunchType = "FARGATE",
                TaskDefinition = taskDefinition.Arn,
                NetworkConfiguration = new ServiceNetworkConfigurationArgs
                {
                    AssignPublicIp = true,
                    Subnets = subnets.Select(x => x.Id).ToList(),
                    SecurityGroups = { securityGroup.Id }
                },
                LoadBalancers =
                {
                    new ServiceLoadBalancerArgs
                    {
                        TargetGroupArn = targetGroup.Arn,
                        ContainerName = stackName,
                        ContainerPort = mapped.TaskDefinitionBackendContainerPort
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
