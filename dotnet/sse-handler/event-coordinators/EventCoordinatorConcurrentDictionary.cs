using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using DotNext;
using Microsoft.Extensions.Logging;
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

        _logger.LogInformation("Device {0}: adding device", id);
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _logger.LogInformation("Device {0}: configuring metrics", id);
        _deviceMetrics.Connected(id);
        var addedValue = _connections[id];
        _logger.LogInformation("Device {0}: added", id);
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

        if (!_connections.TryRemove(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }

        connection!.CancellationTokenSource.Cancel();
        _deviceMetrics.Disconnected(id);
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

        await connection.Stream.WriteAsync(_eventSerializer.SerializeData(message));
        await connection.Stream.FlushAsync();
        _deviceMetrics.Sent(id, message);
        return new Result<bool, EventCoordinatorError>(true);
    }
}
