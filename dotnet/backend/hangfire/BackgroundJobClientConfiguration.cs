using Hangfire;
using SseHandler;

namespace backend.Hangfire;

public static class BackgroundJobClientConfiguration
{
    public static void EnqueueJobs(this WebApplication webApplication)
    {
        var eventCoordinator = webApplication.Services.GetRequiredService<IEventCoordinator>();

        var client = webApplication.Services.GetRequiredService<IRecurringJobManager>();
    }
}
