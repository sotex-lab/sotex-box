using System.Diagnostics.Metrics;
using DotNext;
using DotNext.Collections.Generic;
using Microsoft.Extensions.Logging;
using SseHandler.LoggerExtensions;
using SseHandler.Metrics;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorMutex : IEventCoordinator
{
    private readonly Dictionary<Guid, Connection> _connections;
    private readonly Mutex _lock;
    private readonly IEventSerializer _eventSerializer;
    private readonly IDeviceMetrics _deviceMetrics;
    private readonly ILogger<EventCoordinatorMutex> _logger;

    private static ILogger<EventCoordinatorMutex> GetLogger() =>
        LoggerFactory
            .Create(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Trace);
            })
            .CreateLogger<EventCoordinatorMutex>();

    public EventCoordinatorMutex()
        : this(
            new Dictionary<Guid, Connection>(),
            new JsonEventSerializer(),
            new DeviceMetrics(),
            GetLogger()
        ) { }

    public EventCoordinatorMutex(Dictionary<Guid, Connection> connections, IDeviceMetrics metrics)
        : this(connections, new JsonEventSerializer(), metrics, GetLogger()) { }

    public EventCoordinatorMutex(
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorMutex> logger
    )
        : this(new Dictionary<Guid, Connection>(), eventSerializer, deviceMetrics, logger) { }

    public EventCoordinatorMutex(
        Dictionary<Guid, Connection> connections,
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorMutex> logger
    )
    {
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new Mutex();
        _deviceMetrics = deviceMetrics;
        _logger = logger;
    }

    public Result<CancellationTokenSource, EventCoordinatorError> Add(Guid id, Stream stream)
    {
        if (Guid.Empty.Equals(id))
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

        _lock.WaitOne();
        _logger.LogEventCoordinator(id, "Trying to add device");
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            _lock.ReleaseMutex();
            _logger.LogEventCoordinator(id, "Adding device failed for unknown reasons");
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _logger.LogEventCoordinator(id, "Configuring metrics");
        _deviceMetrics.Connected(id);
        _lock.ReleaseMutex();

        _logger.LogEventCoordinator(id, "Device successfully added");
        return new Result<CancellationTokenSource, EventCoordinatorError>(
            _connections[id].CancellationTokenSource
        );
    }

    public IEnumerable<Guid> GetConnectionIds()
    {
        return _connections.Keys;
    }

    public Result<bool, EventCoordinatorError> Remove(Guid id)
    {
        if (Guid.Empty.Equals(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.InvalidKey);
        }

        if (!_connections.ContainsKey(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }

        _lock.WaitOne();
        _logger.LogEventCoordinator(id, "Trying to remove device");
        var removed = _connections.TryRemove(id);
        if (removed.IsNull)
        {
            _lock.ReleaseMutex();
            _logger.LogEventCoordinator(id, "Removing device failed for unknown reasons");
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }
        _logger.LogEventCoordinator(id, "Removing metrics");

        _deviceMetrics.Disconnected(id);
        _lock.ReleaseMutex();
        removed.Value.CancellationTokenSource.Cancel();
        return new Result<bool, EventCoordinatorError>(true);
    }

    public void RemoveAll()
    {
        foreach (var connection in _connections.Keys)
        {
            Remove(connection);
        }
    }

    public async Task<Result<bool, EventCoordinatorError>> SendMessage(Guid id, object message)
    {
        if (Guid.Empty.Equals(id))
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
