using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SseHandler.EventCoordinators;

namespace SseHandler;

public static class EventCoordinatorExtensions
{
    public static void AddEventCoordinator(this IServiceCollection services)
    {
        services.AddSingleton<IEventCoordinator>(x => new EventCoordinatorConcurrentDictionary(
            x.GetRequiredService<ILogger<EventCoordinatorConcurrentDictionary>>()
        ));
    }
}
