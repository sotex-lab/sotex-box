using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SseHandler.EventCoordinators;
using SseHandler.Metrics;
using SseHandler.Serializers;

namespace SseHandler;

public static class EventCoordinatorExtensions
{
    public static void AddEventCoordinator(this IServiceCollection services)
    {
        services.AddEventCoordinator(new Dictionary<string, Connection>());
    }

    public static void AddEventCoordinator(
        this IServiceCollection services,
        Dictionary<string, Connection> connections
    )
    {
        var serializer = new JsonEventSerializer();

        services.AddDeviceMetrics(serializer);
        services.AddSingleton<IEventCoordinator>(x => new EventCoordinatorMutex(
            connections,
            serializer,
            x.GetRequiredService<IDeviceMetrics>(),
            x.GetRequiredService<ILogger<EventCoordinatorMutex>>()
        ));
    }
}
