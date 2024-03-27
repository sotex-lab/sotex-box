using Microsoft.Extensions.Logging;

namespace SseHandler.LoggerExtensions;

internal static class LoggerExtensions
{
    public static void LogEventCoordinator(
        this ILogger<IEventCoordinator> logger,
        Guid deviceId,
        string message,
        LogLevel level = LogLevel.Debug
    )
    {
        logger.Log(level, "Device {0}: {1}", deviceId, message);
    }
}
