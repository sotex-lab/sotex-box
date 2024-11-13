using DotNext;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using persistence.Repository;
using persistence.Repository.Base;
using Polly;
using Polly.Retry;

namespace persistence;

public static class ApplicationDbContextExtensions
{
    public static void AddSotexBoxDatabase(this IServiceCollection services) =>
        services.AddSotexBoxDatabase(ApplicationDbContextFactory.CONNECTION_STRING);

    public static void AddSotexBoxDatabase(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContextPool<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAdRepository, AdRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
    }

    public static Result<ApplicationDbContext, RepositoryError> Migrate(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>()!;
        var db = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        var response = new Result<ApplicationDbContext, RepositoryError>(db);

        var options = new RetryStrategyOptions
        {
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            MaxRetryAttempts = 5,
            MaxDelay = TimeSpan.FromSeconds(10)
        };

        var pipeline = new ResiliencePipelineBuilder().AddRetry(options).Build();
        try
        {
            pipeline.Execute(() =>
            {
                var pendingMigrations = db.Database.GetPendingMigrations();

                if (pendingMigrations == null || !pendingMigrations.Any())
                {
                    logger.LogInformation("No pending migrations should be applied.");
                    return;
                }

                logger.LogInformation(
                    "Performing {0} migrations: {1}",
                    pendingMigrations.Count(),
                    string.Join(",", pendingMigrations)
                );
                db.Database.Migrate();

                logger.LogInformation("Successfully migrated pending migrations");
            });
        }
        catch (Exception e)
        {
            logger.LogWarning("Caught expcetion while migrating. {0}", e.Message);
            response = new Result<ApplicationDbContext, RepositoryError>(
                RepositoryError.FailedToInit
            );
        }

        return response;
    }
}
