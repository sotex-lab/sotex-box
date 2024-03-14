using Amazon.S3;

namespace backend.Aws;

public static class AwsClientRegistrator
{
    public static void ConfigureAwsClient(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var clientConfig = new AmazonS3Config
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_URL")!,
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
                Environment.GetEnvironmentVariable("AWS_ACCESS_KEY")!,
                Environment.GetEnvironmentVariable("AWS_SECRET_KEY")!,
                clientConfig
            );
        });
    }
}
