using SseHandler;

namespace backend.Hangfire;

public class NoopJob : GenericCronJob<NoopJob>, IGenericCronJob
{
    private readonly IEventCoordinator _eventCoordinator;

    public NoopJob(ILogger<NoopJob> logger, IEventCoordinator eventCoordinator)
        : base(logger)
    {
        _eventCoordinator = eventCoordinator;
    }

    public static string EnvironmentVariableName => "NOOP_CRON";

    public override async Task Run()
    {
        _logger.LogDebug("Sending noop signal to all connections");

        foreach (var device in _eventCoordinator.GetConnectionIds())
        {
            await _eventCoordinator.SendMessage(device, "noop");
        }

        _logger.LogDebug("Sending noop signal finished");
    }
}
