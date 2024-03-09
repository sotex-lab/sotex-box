using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
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
        var postgresContainer = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .Build();
        postgresContainer.StartAsync().GetAwaiter().GetResult();

        Environment.SetEnvironmentVariable(
            "CONNECTION_STRING",
            postgresContainer.GetConnectionString()
        );

        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var eventCoordinatorDescriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Singleton
                && x.ServiceType == typeof(IEventCoordinator)
            );
            services.Remove(eventCoordinatorDescriptor);

            services.AddEventCoordinator(Connections);
        });
    }
}
