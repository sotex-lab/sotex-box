using Microsoft.AspNetCore.Mvc.Testing;
using SseHandler;

public class ConfigurableBackendFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    public Dictionary<string, Connection> Connections { get; set; } =
        new Dictionary<string, Connection>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
