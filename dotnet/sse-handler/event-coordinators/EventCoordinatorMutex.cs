using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNext;
using DotNext.Collections.Generic;
using Microsoft.Extensions.Logging;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorMutex : IEventCoordinator
{
    private readonly ILogger<EventCoordinatorMutex> _logger;
    private readonly Dictionary<string, Connection> _connections;
    private readonly Dictionary<string, Measurement<int>> _measurements;
    private readonly Mutex _lock;
    private readonly IEventSerializer _eventSerializer;

    private static ILogger<EventCoordinatorMutex> _resolveGlobalLogger =>
        LoggerFactory
            .Create(x =>
            {
                x.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger<EventCoordinatorMutex>();

    public EventCoordinatorMutex(Meter meter)
        : this(
            _resolveGlobalLogger,
            new Dictionary<string, Connection>(),
            new JsonEventSerializer(),
            meter
        ) { }

    public EventCoordinatorMutex(Dictionary<string, Connection> connections)
        : this(_resolveGlobalLogger, connections, new JsonEventSerializer(), new Meter("Sotex.Web"))
    { }

    public EventCoordinatorMutex(
        ILogger<EventCoordinatorMutex> logger,
        IEventSerializer eventSerializer,
        IMeterFactory meterFactory
    )
        : this(
            logger,
            new Dictionary<string, Connection>(),
            eventSerializer,
            meterFactory.Create("Sotex.Web")
        ) { }

    public EventCoordinatorMutex(
        ILogger<EventCoordinatorMutex> logger,
        Dictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        Meter meter
    )
    {
        _logger = logger;
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new Mutex();
        _measurements = new Dictionary<string, Measurement<int>>();
        meter.CreateObservableGauge(
            "sotex.web.device",
            () => _measurements.Values,
            "num",
            "The number of devices currently connected to the backend instance"
        );
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
        _measurements[id] = new Measurement<int>(1, new KeyValuePair<string, object?>("id", id));
        _lock.ReleaseMutex();

        return new Result<CancellationTokenSource, EventCoordinatorError>(
            _connections[id].CancellationTokenSource
        );
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
        _measurements[id] = new Measurement<int>(0, new KeyValuePair<string, object?>("id", id));
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
        return new Result<bool, EventCoordinatorError>(true);
    }
}
