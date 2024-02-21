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
    private readonly Mutex _lock;
    private readonly IEventSerializer _eventSerializer;

    private static ILogger<EventCoordinatorMutex> _resolveGlobalLogger =>
        LoggerFactory
            .Create(x =>
            {
                x.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger<EventCoordinatorMutex>();

    public EventCoordinatorMutex()
        : this(
            _resolveGlobalLogger,
            new Dictionary<string, Connection>(),
            new JsonEventSerializer()
        ) { }

    public EventCoordinatorMutex(Dictionary<string, Connection> connections)
        : this(_resolveGlobalLogger, connections, new JsonEventSerializer()) { }

    public EventCoordinatorMutex(
        ILogger<EventCoordinatorMutex> logger,
        IEventSerializer eventSerializer
    )
        : this(logger, new Dictionary<string, Connection>(), eventSerializer) { }

    public EventCoordinatorMutex(
        ILogger<EventCoordinatorMutex> logger,
        Dictionary<string, Connection> connections,
        IEventSerializer eventSerializer
    )
    {
        _logger = logger;
        _connections = connections;
        _eventSerializer = eventSerializer;
        _lock = new Mutex();
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
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.DuplicateKey);
        }

        _lock.WaitOne();
        var removed = _connections.TryRemove(id);
        if (removed.IsNull)
        {
            _lock.ReleaseMutex();
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }
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
