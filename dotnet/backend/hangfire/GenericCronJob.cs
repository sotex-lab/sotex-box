using NCrontab;

namespace backend.Hangfire;

public interface IGenericCronJob
{
    static abstract string EnvironmentVariableName { get; }
}

public abstract class GenericCronJob<T>
    where T : IGenericCronJob
{
    private static string defaultCron = "0/15 * * ? * *";
    protected readonly ILogger<GenericCronJob<T>> _logger;

    public static string Cron()
    {
        var value = Environment.GetEnvironmentVariable(T.EnvironmentVariableName);
        if (value == null)
            return defaultCron;

        try
        {
            var parsed = CrontabSchedule.Parse(
                value,
                new CrontabSchedule.ParseOptions { IncludingSeconds = true }
            );
            return value;
        }
        catch (CrontabException)
        {
            return defaultCron;
        }
    }

    public GenericCronJob(ILogger<GenericCronJob<T>> logger)
    {
        _logger = logger;
    }

    public abstract Task Run();
}
