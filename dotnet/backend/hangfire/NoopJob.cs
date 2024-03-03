using SseHandler;

namespace backend.Hangfire;

public class NoopJob
{
    private readonly ILogger<NoopJob> _logger;
    private readonly IEventCoordinator _eventCoordinator;

    public NoopJob(ILogger<NoopJob> logger, IEventCoordinator eventCoordinator)
    {
        _logger = logger;
        _eventCoordinator = eventCoordinator;
    }

    public async Task SendNoop()
    {
        _logger.LogInformation("Sending noop signal to all connections");

        foreach (var device in _eventCoordinator.GetConnectionIds())
        {
            await _eventCoordinator.SendMessage(device, "noop");
        }

        _logger.LogInformation("Sending noop signal finished");
    }
}
