using Amazon.S3;
using Amazon.SQS;

namespace backend.Aws;

public static class AwsClientRegistrator
{
    public static void ConfigureAwsClients(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var clientConfig = new AmazonS3Config
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_S3_URL")!,
                AuthenticationRegion = Environment.GetEnvironmentVariable("AWS_REGION")!,
                ForcePathStyle = true,
            };

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")! == "test")
            {
                clientConfig.ProxyHost = Environment.GetEnvironmentVariable("AWS_PROXY_HOST")!;
                clientConfig.ProxyPort = int.Parse(
                    Environment.GetEnvironmentVariable("AWS_PROXY_PORT")!
                );
                clientConfig.UseHttp = true;
            }

            return new AmazonS3Client(
                Environment.GetEnvironmentVariable("AWS_S3_ACCESS_KEY")!,
                Environment.GetEnvironmentVariable("AWS_S3_SECRET_KEY")!,
                clientConfig
            );
        });

        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var clientConfig = new AmazonSQSConfig
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_SQS_URL")!,
                AuthenticationRegion = Environment.GetEnvironmentVariable("AWS_REGION")!,
            };

            return new AmazonSQSClient(
                Environment.GetEnvironmentVariable("AWS_SQS_ACCESS_KEY")!,
                Environment.GetEnvironmentVariable("AWS_SQS_SECRET_KEY")!,
                clientConfig
            );
        });
    }
}
