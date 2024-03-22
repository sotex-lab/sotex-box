using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNext;
using DotNext.Collections.Generic;
using DotNext.Threading;
using Microsoft.Extensions.Logging;
using SseHandler.LoggerExtensions;
using SseHandler.Metrics;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorReaderWriterLock : IEventCoordinator
{
    private readonly Dictionary<Guid, Connection> _connections;
    private readonly ReaderWriterSpinLock _lock;
    private readonly IEventSerializer _eventSerializer;
    private readonly IDeviceMetrics _deviceMetrics;
    private readonly ILogger<EventCoordinatorReaderWriterLock> _logger;

    private static ILogger<EventCoordinatorReaderWriterLock> GetLogger() =>
        LoggerFactory
            .Create(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Trace);
            })
            .CreateLogger<EventCoordinatorReaderWriterLock>();

    public EventCoordinatorReaderWriterLock()
        : this(
            new Dictionary<Guid, Connection>(),
            new JsonEventSerializer(),
            new DeviceMetrics(),
            GetLogger()
        ) { }

    public EventCoordinatorReaderWriterLock(
        Dictionary<Guid, Connection> connections,
        IDeviceMetrics metrics
    )
        : this(connections, new JsonEventSerializer(), metrics, GetLogger()) { }

    public EventCoordinatorReaderWriterLock(
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorReaderWriterLock> logger
    )
        : this(new Dictionary<Guid, Connection>(), eventSerializer, deviceMetrics, logger) { }

    public EventCoordinatorReaderWriterLock(
        Dictionary<Guid, Connection> connections,
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorReaderWriterLock> logger
    )
    {
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new ReaderWriterSpinLock();
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

        _lock.EnterReadLock();
        if (_connections.ContainsKey(id))
        {
            _lock.ExitReadLock();
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.DuplicateKey
            );
        }

        _lock.EnterWriteLock();
        _logger.LogEventCoordinator(id, "Trying to add device");
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            _lock.ExitWriteLock();
            _lock.ExitReadLock();
            _logger.LogEventCoordinator(id, "Adding device failed for unknown reasons");
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _logger.LogEventCoordinator(id, "Configuring metrics");
        var result = new Result<CancellationTokenSource, EventCoordinatorError>(
            _connections[id].CancellationTokenSource
        );
        _lock.ExitWriteLock();
        _lock.ExitReadLock();
        _deviceMetrics.Connected(id);
        _logger.LogEventCoordinator(id, "Device successfully added");
        return result;
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

        _lock.EnterReadLock();
        if (!_connections.ContainsKey(id))
        {
            _lock.ExitReadLock();
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }

        _lock.EnterWriteLock();
        _logger.LogEventCoordinator(id, "Trying to remove device");
        var removed = _connections.TryRemove(id);
        if (removed.IsNull)
        {
            _lock.ExitWriteLock();
            _lock.ExitReadLock();
            _logger.LogEventCoordinator(id, "Removing device failed for unknown reasons");
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }
        removed.Value.CancellationTokenSource.Cancel();
        _logger.LogEventCoordinator(id, "Removing metrics");
        _deviceMetrics.Disconnected(id);
        _lock.ExitWriteLock();
        _lock.ExitReadLock();
        _logger.LogEventCoordinator(id, "Device successfully removed");
        return new Result<bool, EventCoordinatorError>(true);
    }

    public void RemoveAll()
    {
        _lock.EnterReadLock();

        foreach (var connection in _connections.Keys)
        {
            Remove(connection);
        }

        _lock.EnterReadLock();
    }

    public async Task<Result<bool, EventCoordinatorError>> SendMessage(Guid id, object message)
    {
        if (Guid.Empty.Equals(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.InvalidKey);
        }
        _lock.EnterReadLock();
        if (!_connections.TryGetValue(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }
        _logger.LogEventCoordinator(id, "Sending message");

        await connection.Stream.WriteAsync(_eventSerializer.SerializeData(message));
        await connection.Stream.FlushAsync();
        _lock.ExitReadLock();
        _logger.LogEventCoordinator(id, "Updating message metrics");
        _deviceMetrics.Sent(id, message);
        _logger.LogEventCoordinator(id, "Message successfully sent");
        return new Result<bool, EventCoordinatorError>(true);
    }
}
