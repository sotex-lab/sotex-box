using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using persistence;
using SseHandler;
using Testcontainers.PostgreSql;

public class ConfigurableBackendFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    public Dictionary<string, Connection> Connections { get; set; } =
        new Dictionary<string, Connection>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "test");

        var postgresContainer = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .Build();
        postgresContainer.StartAsync().GetAwaiter().GetResult();

        // We need to put here something because it has a check
        // for the validity of connection string. Value from here
        // gets overriden in the following code.
        Environment.SetEnvironmentVariable(
            "CONNECTION_STRING",
            postgresContainer.GetConnectionString()
        );

        builder.ConfigureServices(services =>
        {
            var eventCoordinatorDescriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Singleton
                && x.ServiceType == typeof(IEventCoordinator)
            );
            services.Remove(eventCoordinatorDescriptor);

            services.AddEventCoordinator(Connections);

            var databaseDescriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Scoped
                && x.ServiceType == typeof(ApplicationDbContext)
            );
            services.Remove(databaseDescriptor);

            new ApplicationDbContextFactory()
                .CreateDbContext(postgresContainer.GetConnectionString())
                .Database.Migrate();

            services.AddSotexBoxDatabase(postgresContainer.GetConnectionString());
        });
    }
}
