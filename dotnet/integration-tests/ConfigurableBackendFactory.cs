using System.Data.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using persistence;
using Respawn;
using SseHandler;
using Testcontainers.PostgreSql;

public class ConfigurableBackendFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    public const string IntegrationCollection = "integration collection";
    public Dictionary<string, Connection> Connections { get; set; } =
        new Dictionary<string, Connection>();

    private Respawner? respawner;
    private DbConnection? connection;

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

        var respawnerOptions = new RespawnerOptions
        {
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        };

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

            var context = new ApplicationDbContextFactory().CreateDbContext(
                postgresContainer.GetConnectionString()
            );

            context.Database.Migrate();

            connection = context.Database.GetDbConnection();
            connection.Open();

            respawner = Respawner
                .CreateAsync(connection, respawnerOptions)
                .GetAwaiter()
                .GetResult();

            services.AddSotexBoxDatabase(postgresContainer.GetConnectionString());
        });
    }

    public void ResetDatabase() => respawner!.ResetAsync(connection!).GetAwaiter().GetResult();
}

[CollectionDefinition(ConfigurableBackendFactory.IntegrationCollection)]
public class BackendCollection : ICollectionFixture<ConfigurableBackendFactory> { }
