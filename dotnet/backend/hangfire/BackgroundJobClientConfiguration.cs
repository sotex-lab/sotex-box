using Hangfire;

namespace backend.Hangfire;

public static class BackgroundJobClientConfiguration
{
    public static void EnqueueJobs(this WebApplication webApplication)
    {
        var client = webApplication.Services.GetRequiredService<IRecurringJobManager>();

        client.AddOrUpdate<NoopJob>("noop", noopJob => noopJob.Run(), NoopJob.Cron());
        client.AddOrUpdate<SqsProcessorJob>(
            "sqs",
            sqsProcessorJob => sqsProcessorJob.Run(),
            SqsProcessorJob.Cron()
        );
    }
}
