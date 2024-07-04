using System;
using System.Collections.Generic;
using ConfigTranslator.Attributes;

public class MappedConfig
{
    [String("networking-vpc-cidrBlock")]
    public string NetworkingVpcCidrBlock { get; set; }

    [String("networking-subnets-cidrBlock", true)]
    public IEnumerable<string>? NetworkingSubnetsCidrBlock { get; set; }

    [String("networking-subnets-availabilityZone", true)]
    public IEnumerable<string>? NetworkingSubnetsAvailabilityZone { get; set; }

    [String("buckets", true)]
    public IEnumerable<string>? Buckets { get; set; }

    [String("sqsProcessorQueue")]
    public string SqsProcessorQueue { get; set; }

    [Int("dbAllocatedStorage")]
    public int DbAllocatedStorage { get; set; }

    [String("dbEngine")]
    public string DbEngine { get; set; }

    [String("dbEngineVersion")]
    public string DbEngineVersion { get; set; }

    [String("dbInstanceClass")]
    public string DbInstanceClass { get; set; }

    [String("ubuntuAmi")]
    public string UbuntuAmi { get; set; }

    [String("publicKey")]
    public string PublicKey { get; set; }

    [String("ec2InstanceType")]
    public string Ec2InstanceType { get; set; }

    [String("sqsUrl")]
    public string SqsUrl { get; set; }

    [String("s3Url")]
    public string S3Url { get; set; }
}
