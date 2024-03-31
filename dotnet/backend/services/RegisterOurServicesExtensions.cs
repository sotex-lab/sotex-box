using Amazon.S3;
using backend.Services.Aws;
using backend.Services.Batching;

namespace backend.Services;

public static class RegisterOurServicesExtensions
{
    public static void RegisterOurServices(this IServiceCollection services)
    {
        services.AddTransient<IGetOrCreateBucketService, GetOrCreateBucketServiceImpl>();
        services.AddTransient<IPutObjectService, PutObjectServiceImpl>();

        services.AddTransient<IPreSignObjectService>(sp => new PreSignObjectServiceImpl(
            sp.GetService<IAmazonS3>()!,
            sp.GetService<ILogger<PreSignObjectServiceImpl>>()!,
            Environment.GetEnvironmentVariable("AWS_PROTOCOL")!
        ));

        services.AddSingleton<IDeviceBatcher<Guid>>(new DeviceBatcher<Guid>());
    }
}
