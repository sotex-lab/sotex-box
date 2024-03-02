using System.Diagnostics.Metrics;
using DotNext;
using DotNext.Collections.Generic;
using SseHandler.Metrics;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorMutex : IEventCoordinator
{
    private readonly Dictionary<string, Connection> _connections;
    private readonly Mutex _lock;
    private readonly IEventSerializer _eventSerializer;
    private readonly IDeviceMetrics _deviceMetrics;

    public EventCoordinatorMutex()
        : this(new Dictionary<string, Connection>(), new JsonEventSerializer(), new DeviceMetrics())
    { }

    public EventCoordinatorMutex(Dictionary<string, Connection> connections, IDeviceMetrics metrics)
        : this(connections, new JsonEventSerializer(), metrics) { }

    public EventCoordinatorMutex(IEventSerializer eventSerializer, IDeviceMetrics deviceMetrics)
        : this(new Dictionary<string, Connection>(), eventSerializer, deviceMetrics) { }

    public EventCoordinatorMutex(
        Dictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics
    )
    {
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new Mutex();
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

        if (_connections.ContainsKey(id))
        {
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.DuplicateKey
            );
        }

        _lock.WaitOne();
        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            _lock.ReleaseMutex();
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _deviceMetrics.Connected(id);
        _lock.ReleaseMutex();

        return new Result<CancellationTokenSource, EventCoordinatorError>(
            _connections[id].CancellationTokenSource
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

        _lock.WaitOne();
        var removed = _connections.TryRemove(id);
        if (removed.IsNull)
        {
            _lock.ReleaseMutex();
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }
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
