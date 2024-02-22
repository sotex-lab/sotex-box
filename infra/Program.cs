using System.Threading.Tasks;
using Pulumi;
using Pulumi.Aws.ElasticBeanstalk;

class SotexBoxStack : Stack
{
    public SotexBoxStack()
    {
        // aws fargate
    }
}

class Program
{
    static Task<int> Main(string[] args) => Deployment.RunAsync<SotexBoxStack>();
}
