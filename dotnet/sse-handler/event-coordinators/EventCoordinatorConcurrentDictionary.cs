using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using DotNext;
using Microsoft.Extensions.Logging;
using SseHandler.LoggerExtensions;
using SseHandler.Metrics;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorConcurrentDictionary : IEventCoordinator
{
    private readonly ConcurrentDictionary<string, Connection> _connections;
    private readonly IDeviceMetrics _deviceMetrics;
    private readonly IEventSerializer _eventSerializer;
    private readonly ILogger<EventCoordinatorConcurrentDictionary> _logger;

    private static ILogger<EventCoordinatorConcurrentDictionary> GetLogger() =>
        LoggerFactory
            .Create(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Trace);
            })
            .CreateLogger<EventCoordinatorConcurrentDictionary>();

    public EventCoordinatorConcurrentDictionary()
        : this(
            new ConcurrentDictionary<string, Connection>(),
            new JsonEventSerializer(),
            new DeviceMetrics(),
            GetLogger()
        ) { }

    public EventCoordinatorConcurrentDictionary(
        ConcurrentDictionary<string, Connection> connections,
        IDeviceMetrics metrics
    )
        : this(connections, new JsonEventSerializer(), metrics, GetLogger()) { }

    public EventCoordinatorConcurrentDictionary(
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorConcurrentDictionary> logger
    )
        : this(
            new ConcurrentDictionary<string, Connection>(),
            eventSerializer,
            deviceMetrics,
            logger
        ) { }

    public EventCoordinatorConcurrentDictionary(
        ConcurrentDictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorConcurrentDictionary> logger
    )
    {
        _connections = connections;
        _eventSerializer = eventSerializer;
        _deviceMetrics = deviceMetrics;
        _logger = logger;
    }

    public Result<CancellationTokenSource, EventCoordinatorError> Add(string id, Stream stream)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.InvalidKey
            );
        }

        if (_connections.ContainsKey(id))
        {
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.DuplicateKey
            );
        }

        _logger.LogEventCoordinator(id, "Trying to add device");
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            _logger.LogEventCoordinator(id, "Adding device failed for unknown reasons");
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _logger.LogInformation("Device {0}: configuring metrics", id);
        _deviceMetrics.Connected(id);
        var addedValue = _connections[id];
        _logger.LogEventCoordinator(id, "Device successfully added");
        return new Result<CancellationTokenSource, EventCoordinatorError>(
            addedValue.CancellationTokenSource
        );
    }

    public IEnumerable<string> GetConnectionIds()
    {
        return _connections.Keys;
    }

    public Result<bool, EventCoordinatorError> Remove(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.InvalidKey);
        }

        if (!_connections.ContainsKey(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }
        _logger.LogEventCoordinator(id, "Trying to remove device");
        if (!_connections.TryRemove(id, out var connection))
        {
            _logger.LogEventCoordinator(id, "Removing device failed for unknown reasons");
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }
        _logger.LogEventCoordinator(id, "Removing metrics");
        connection!.CancellationTokenSource.Cancel();
        _deviceMetrics.Disconnected(id);
        _logger.LogEventCoordinator(id, "Device successfully removed");
        return new Result<bool, EventCoordinatorError>(true);
    }

    public void RemoveAll()
    {
        foreach (var connection in _connections.Keys)
        {
            Remove(connection);
        }
    }

    public async Task<Result<bool, EventCoordinatorError>> SendMessage(string id, object message)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.InvalidKey);
        }

        if (!_connections.TryGetValue(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }
        _logger.LogEventCoordinator(id, "Sending message");
        await connection.Stream.WriteAsync(_eventSerializer.SerializeData(message));
        await connection.Stream.FlushAsync();
        _logger.LogEventCoordinator(id, "Updating message metrics");
        _deviceMetrics.Sent(id, message);
        _logger.LogEventCoordinator(id, "Message successfully sent");
        return new Result<bool, EventCoordinatorError>(true);
    }
}
