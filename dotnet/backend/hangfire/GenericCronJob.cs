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

    public static string Cron() =>
        Environment.GetEnvironmentVariable(T.EnvironmentVariableName) ?? defaultCron;

    public GenericCronJob(ILogger<GenericCronJob<T>> logger)
    {
        _logger = logger;
    }

    public abstract Task Run();
}
