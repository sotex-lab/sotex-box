using DotNetRetry.Rules;
using DotNext;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using persistence.Repository;
using persistence.Repository.Base;

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
    }

    public static Result<ApplicationDbContext, RepositoryError> Migrate(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>()!;
        var db = scope.ServiceProvider.GetService<ApplicationDbContext>()!;

        var rule = Rule.Setup(Strategy.Exponential)
            .Config(options =>
            {
                options.Attempts = 3;
                options.Time = TimeSpan.FromSeconds(5);
            });
        var response = new Result<ApplicationDbContext, RepositoryError>(db);

        rule.OnFailure(
                (_, _) =>
                {
                    response = new Result<ApplicationDbContext, RepositoryError>(
                        RepositoryError.FailedToInit
                    );
                }
            )
            .Attempt(() =>
            {
                try
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
                }
                catch (Exception)
                {
                    logger.LogWarning("Caught expcetion while migrating. Retrying...");
                }
            });
        return response;
    }
}
