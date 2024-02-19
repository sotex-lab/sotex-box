using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNext;
using Microsoft.Extensions.Logging;

namespace SseHandler.EventCoordinators;

public class EventCoordinatorConcurrentDictionary : IEventCoordinator
{
    private readonly ILogger<EventCoordinatorConcurrentDictionary> _logger;
    private readonly ConcurrentDictionary<string, Connection> _connections;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private static ILogger<EventCoordinatorConcurrentDictionary> _resolveGlobalLogger =>
        LoggerFactory
            .Create(x =>
            {
                x.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger<EventCoordinatorConcurrentDictionary>();

    public EventCoordinatorConcurrentDictionary()
        : this(_resolveGlobalLogger, new ConcurrentDictionary<string, Connection>()) { }

    public EventCoordinatorConcurrentDictionary(
        ConcurrentDictionary<string, Connection> connections
    )
        : this(_resolveGlobalLogger, connections) { }

    public EventCoordinatorConcurrentDictionary(
        ILogger<EventCoordinatorConcurrentDictionary> logger
    )
        : this(logger, new ConcurrentDictionary<string, Connection>()) { }

    public EventCoordinatorConcurrentDictionary(
        ILogger<EventCoordinatorConcurrentDictionary> logger,
        ConcurrentDictionary<string, Connection> connections
    )
    {
        _logger = logger;
        _connections = connections;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };
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
            _logger.LogTrace("Connections doesn't contain key {id}", id);
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
            _logger.LogTrace("Connections doesn't contain key {id}", id);
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }

        if (!_connections.TryRemove(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.Unknown);
        }

        connection!.CancellationTokenSource.Cancel();
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
        var bytes = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(message, _jsonSerializerOptions) + "\n"
        );
        if (!_connections.TryGetValue(id, out var connection))
        {
            return new Result<bool, EventCoordinatorError>(EventCoordinatorError.KeyNotFound);
        }

        await connection.Stream.WriteAsync(bytes);
        await connection.Stream.FlushAsync();
        return new Result<bool, EventCoordinatorError>(true);
    }
}
