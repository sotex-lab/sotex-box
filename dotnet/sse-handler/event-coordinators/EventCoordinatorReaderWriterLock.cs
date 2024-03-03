using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNext;
using DotNext.Collections.Generic;
using DotNext.Threading;
using Microsoft.Extensions.Logging;
using SseHandler.Metrics;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorReaderWriterLock : IEventCoordinator
{
    private readonly Dictionary<string, Connection> _connections;
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
            new Dictionary<string, Connection>(),
            new JsonEventSerializer(),
            new DeviceMetrics(),
            GetLogger()
        ) { }

    public EventCoordinatorReaderWriterLock(
        Dictionary<string, Connection> connections,
        IDeviceMetrics metrics
    )
        : this(connections, new JsonEventSerializer(), metrics, GetLogger()) { }

    public EventCoordinatorReaderWriterLock(
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics,
        ILogger<EventCoordinatorReaderWriterLock> logger
    )
        : this(new Dictionary<string, Connection>(), eventSerializer, deviceMetrics, logger) { }

    public EventCoordinatorReaderWriterLock(
        Dictionary<string, Connection> connections,
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

    public Result<CancellationTokenSource, EventCoordinatorError> Add(string id, Stream stream)
    {
        if (string.IsNullOrEmpty(id))
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
        _logger.LogInformation("Device {0}: adding device", id);
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            _lock.ExitWriteLock();
            _lock.ExitReadLock();
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _logger.LogInformation("Device {0}: configuring metrics", id);
        var result = new Result<CancellationTokenSource, EventCoordinatorError>(
            _connections[id].CancellationTokenSource
        );
        _lock.ExitWriteLock();
        _lock.ExitReadLock();
        _deviceMetrics.Connected(id);
        _logger.LogInformation("Device {0}: added", id);
        return result;
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

        _lock.EnterReadLock();
        if (!_connections.ContainsKey(id))
        {
            _lock.ExitReadLock();
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }

        _lock.EnterWriteLock();
        var removed = _connections.TryRemove(id);
        if (removed.IsNull)
        {
            _lock.ExitWriteLock();
            _lock.ExitReadLock();
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }
        removed.Value.CancellationTokenSource.Cancel();
        _deviceMetrics.Disconnected(id);
        _lock.ExitWriteLock();
        _lock.ExitReadLock();
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

    public async Task<Result<bool, EventCoordinatorError>> SendMessage(string id, object message)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.InvalidKey);
        }
        _lock.EnterReadLock();
        if (!_connections.TryGetValue(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }

        await connection.Stream.WriteAsync(_eventSerializer.SerializeData(message));
        await connection.Stream.FlushAsync();
        _lock.ExitReadLock();
        _deviceMetrics.Sent(id, message);
        return new Result<bool, EventCoordinatorError>(true);
    }
}
