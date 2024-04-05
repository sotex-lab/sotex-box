using Hangfire;

namespace backend.Hangfire;

public static class BackgroundJobClientConfiguration
{
    public static void EnqueueJobs(this WebApplication webApplication)
    {
        var recurringClient = webApplication.Services.GetRequiredService<IRecurringJobManager>();

        recurringClient.AddOrUpdate<NoopJob>("noop", noopJob => noopJob.Run(), NoopJob.Cron());
        recurringClient.AddOrUpdate<SqsProcessorJob>(
            "sqs",
            sqsProcessorJob => sqsProcessorJob.Run(),
            SqsProcessorJob.Cron()
        );

        var standardClient = webApplication.Services.GetRequiredService<IBackgroundJobClientV2>();
        standardClient.Enqueue<CallForScheduleJob>(job => job.Run());
    }
}
