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

    public EventCoordinatorReaderWriterLock()
        : this(new Dictionary<string, Connection>(), new JsonEventSerializer(), new DeviceMetrics())
    { }

    public EventCoordinatorReaderWriterLock(Dictionary<string, Connection> connections)
        : this(connections, new JsonEventSerializer(), new DeviceMetrics()) { }

    public EventCoordinatorReaderWriterLock(
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics
    )
        : this(new Dictionary<string, Connection>(), eventSerializer, deviceMetrics) { }

    public EventCoordinatorReaderWriterLock(
        Dictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics
    )
    {
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new ReaderWriterSpinLock();
        _deviceMetrics = deviceMetrics;
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
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            _lock.ExitWriteLock();
            _lock.ExitReadLock();
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }

        var result = new Result<CancellationTokenSource, EventCoordinatorError>(
            _connections[id].CancellationTokenSource
        );
        _lock.ExitWriteLock();
        _lock.ExitReadLock();
        _deviceMetrics.Connected(id);
        return result;
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
        return new Result<bool, EventCoordinatorError>(true);
    }
}
