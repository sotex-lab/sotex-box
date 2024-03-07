using Hangfire;
using Hangfire.Common;
using SseHandler;

namespace backend.Hangfire;

public static class BackgroundJobClientConfiguration
{
    public static void EnqueueJobs(this WebApplication webApplication)
    {
        var client = webApplication.Services.GetRequiredService<IRecurringJobManager>();

        client.AddOrUpdate<NoopJob>("noop", noopJob => noopJob.SendNoop(), NoopJob.Cron());
    }
}
