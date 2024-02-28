using System.Collections.Generic;
using ConfigTranslator.Attributes;

public class MappedConfig
{
    [String("networking-vpc-cidrBlock")]
    public string NetworkingVpcCidrBlock { get; set; }

    [String("networking-publicRoutingTable-cidrBlock")]
    public string NetworkingPublicRoutingTableCidrBlock { get; set; }

    [String("networking-subnets-cidrBlock", true)]
    public IEnumerable<string>? NetworkingSubnetsCidrBlock { get; set; }

    [String("networking-subnets-availabilityZone", true)]
    public IEnumerable<string>? NetworkingSubnetsAvailabilityZone { get; set; }

    [String("networking-securityGroup-egress-protocol")]
    public string NetworkingSecurityGroupEgressProtocol { get; set; }

    [Int("networking-securityGroup-egress-fromPort")]
    public int NetworkingSecurityGroupEgressFromPort { get; set; }

    [Int("networking-securityGroup-egress-toPort")]
    public int NetworkingSecurityGroupEgressToPort { get; set; }

    [String("networking-securityGroup-egress-cidrBlocks")]
    public string NetworkingSecurityGroupEgressCidrBlocks { get; set; }

    [String("networking-securityGroup-ingress-protocol")]
    public string NetworkingSecurityGroupIngressProtocol { get; set; }

    [String("networking-securityGroup-ingress-cidrBlocks")]
    public string NetworkingSecurityGroupIngressCidrBlocks { get; set; }

    [Int("networking-securityGroup-ingress-fromPort")]
    public int NetworkingSecurityGroupIngressFromPort { get; set; }

    [Int("networking-securityGroup-ingress-toPort")]
    public int NetworkingSecurityGroupIngressToPort { get; set; }

    [String("networking-targetGroup-protocol")]
    public string NetworkingTargetGroupProtocol { get; set; }

    [Int("networking-targetGroup-port")]
    public int NetworkingTargetGroupPort { get; set; }

    [String("networking-targetGroup-targetType")]
    public string NetworkingTargetGroupTargetType { get; set; }

    [Int("networking-listener-port")]
    public int NetworkingListenerPort { get; set; }

    [String("networking-listener-actionType")]
    public string NetworkingListenerActionType { get; set; }

    [String("rolePolicy-version")]
    public string RolePolicyVersion { get; set; }

    [String("rolePolicy-statement-sid")]
    public string RolePolicyStatementSid { get; set; }

    [String("rolePolicy-statement-effect")]
    public string RolePolicyStatementEffect { get; set; }

    [String("rolePolicy-statement-principalService")]
    public string RolePolicyStatementPrincipalService { get; set; }

    [String("rolePolicy-statement-action")]
    public string RolePolicyStatementAction { get; set; }

    [String("rolePolicy-policyArn")]
    public string RolePolicyPolicyArn { get; set; }

    [String("taskDefinition-cpu")]
    public string TaskDefinitionCpu { get; set; }

    [String("taskDefinition-memory")]
    public string TaskDefinitionMemory { get; set; }

    [String("taskDefinition-backend-image")]
    public string TaskDefintionBackendImage { get; set; }

    [Int("taskDefinition-backend-containerPort")]
    public int TaskDefinitionBackendContainerPort { get; set; }

    [Int("taskDefinition-backend-hostPort")]
    public int TaskDefinitionBackendHostPort { get; set; }

    [String("taskDefinition-backend-protocol")]
    public string TaskDefinitionBackendProtocol { get; set; }

    [Int("service-desiredCount")]
    public int ServiceDesiredCount { get; set; }
}
