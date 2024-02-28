using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SseHandler.EventCoordinators;
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
        services.AddSingleton<IEventCoordinator>(x => new EventCoordinatorMutex(
            x.GetRequiredService<ILogger<EventCoordinatorMutex>>(),
            connections,
            new JsonEventSerializer(),
            x.GetRequiredService<IMeterFactory>().Create("Sotex.Web")
        ));
    }
}
