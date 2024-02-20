using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SseHandler.EventCoordinators;
using SseHandler.Serializers;

namespace SseHandler;

public static class EventCoordinatorExtensions
{
    public static void AddEventCoordinator(this IServiceCollection services)
    {
        services.AddSingleton<IEventCoordinator>(x => new EventCoordinatorMutex(
            x.GetRequiredService<ILogger<EventCoordinatorMutex>>(),
            new JsonEventSerializer()
        ));
    }
}
