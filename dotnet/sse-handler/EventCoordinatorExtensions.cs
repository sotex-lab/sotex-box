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
        services.AddEventCoordinator(new Dictionary<Guid, Connection>());
    }

    public static void AddEventCoordinator(
        this IServiceCollection services,
        Dictionary<Guid, Connection> connections
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
