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

    public EventCoordinatorConcurrentDictionary()
        : this(
            new ConcurrentDictionary<string, Connection>(),
            new JsonEventSerializer(),
            new DeviceMetrics()
        ) { }

    public EventCoordinatorConcurrentDictionary(
        ConcurrentDictionary<string, Connection> connections,
        IDeviceMetrics metrics
    )
        : this(connections, new JsonEventSerializer(), metrics) { }

    public EventCoordinatorConcurrentDictionary(
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics
    )
        : this(new ConcurrentDictionary<string, Connection>(), eventSerializer, deviceMetrics) { }

    public EventCoordinatorConcurrentDictionary(
        ConcurrentDictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        IDeviceMetrics deviceMetrics
    )
    {
        _connections = connections;
        _eventSerializer = eventSerializer;
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

        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _deviceMetrics.Connected(id);
        var addedValue = _connections[id];
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
