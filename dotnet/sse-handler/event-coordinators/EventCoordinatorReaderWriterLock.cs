using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNext;
using DotNext.Collections.Generic;
using DotNext.Threading;
using Microsoft.Extensions.Logging;
using SseHandler.Serializers;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorReaderWriterLock : IEventCoordinator
{
    private readonly ILogger<EventCoordinatorReaderWriterLock> _logger;
    private readonly Dictionary<string, Connection> _connections;
    private readonly Dictionary<string, Measurement<int>> _measurements;
    private readonly ReaderWriterSpinLock _lock;
    private readonly IEventSerializer _eventSerializer;

    private static ILogger<EventCoordinatorReaderWriterLock> _resolveGlobalLogger =>
        LoggerFactory
            .Create(x =>
            {
                x.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger<EventCoordinatorReaderWriterLock>();

    public EventCoordinatorReaderWriterLock(Meter meter)
        : this(
            _resolveGlobalLogger,
            new Dictionary<string, Connection>(),
            new JsonEventSerializer(),
            meter
        ) { }

    public EventCoordinatorReaderWriterLock(Dictionary<string, Connection> connections)
        : this(_resolveGlobalLogger, connections, new JsonEventSerializer(), new Meter("Sotex.Web"))
    { }

    public EventCoordinatorReaderWriterLock(
        ILogger<EventCoordinatorReaderWriterLock> logger,
        IEventSerializer eventSerializer,
        IMeterFactory meterFactory
    )
        : this(
            logger,
            new Dictionary<string, Connection>(),
            eventSerializer,
            meterFactory.Create("Sotex.Web")
        ) { }

    public EventCoordinatorReaderWriterLock(
        ILogger<EventCoordinatorReaderWriterLock> logger,
        Dictionary<string, Connection> connections,
        IEventSerializer eventSerializer,
        Meter meter
    )
    {
        _logger = logger;
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new ReaderWriterSpinLock();
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
        _measurements[id] = new Measurement<int>(1, new KeyValuePair<string, object?>("id", id));
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
        _measurements[id] = new Measurement<int>(0, new KeyValuePair<string, object?>("id", id));
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
