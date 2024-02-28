using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using DotNext;
using Microsoft.Extensions.Logging;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorConcurrentDictionary : IEventCoordinator
{
    private readonly ILogger<EventCoordinatorConcurrentDictionary> _logger;
    private readonly ConcurrentDictionary<string, Connection> _connections;
    private readonly Dictionary<string, Measurement<int>> _measurements;
    private readonly IEventSerializer _eventSerializer;
    private static ILogger<EventCoordinatorConcurrentDictionary> _resolveGlobalLogger =>
        LoggerFactory
            .Create(x =>
            {
                x.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger<EventCoordinatorConcurrentDictionary>();

    public EventCoordinatorConcurrentDictionary(Meter meter)
        : this(
            _resolveGlobalLogger,
            new ConcurrentDictionary<string, Connection>(),
            new JsonEventSerializer(),
            meter
        ) { }

    public EventCoordinatorConcurrentDictionary(
        ConcurrentDictionary<string, Connection> connections
    )
        : this(_resolveGlobalLogger, connections, new JsonEventSerializer(), new Meter("Sotex.Web"))
    { }

    public EventCoordinatorConcurrentDictionary(
        ILogger<EventCoordinatorConcurrentDictionary> logger,
        IEventSerializer eventSerializer,
        IMeterFactory meterFactory
    )
        : this(
            logger,
            new ConcurrentDictionary<string, Connection>(),
            eventSerializer,
            meterFactory.Create("Sotex.Web")
        ) { }

    public EventCoordinatorConcurrentDictionary(
        ILogger<EventCoordinatorConcurrentDictionary> logger,
        ConcurrentDictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        Meter meter
    )
    {
        _logger = logger;
        _connections = connections;
        _eventSerializer = eventSerializer;
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

        if (!_connections.TryAdd(id, new Connection(id, stream)))
        {
            return new Result<CancellationTokenSource, EventCoordinatorError>(
                EventCoordinatorError.Unknown
            );
        }
        _measurements[id] = new Measurement<int>(1, new KeyValuePair<string, object?>("id", id));
        var addedValue = _connections[id];
        return new Result<CancellationTokenSource, EventCoordinatorError>(
            addedValue.CancellationTokenSource
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

        if (!_connections.TryRemove(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }

        connection!.CancellationTokenSource.Cancel();
        _measurements[id] = new Measurement<int>(0, new KeyValuePair<string, object?>("id", id));
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
