using System.Text;
using DotNext;

namespace SseHandler;

public interface IEventCoordinator
{
    Result<CancellationTokenSource, EventCoordinatorError> Add(Guid id, Stream stream);
    Task<Result<bool, EventCoordinatorError>> SendMessage(Guid id, object message);
    Result<bool, EventCoordinatorError> Remove(Guid id);
    void RemoveAll();
    IEnumerable<Guid> GetConnectionIds();
}

public enum EventCoordinatorError
{
    Unknown,
    KeyNotFound,
    InvalidKey,
    DuplicateKey,
}

public static class EventCoordinatorErrorExtension
{
    public static string Stringify(this EventCoordinatorError err) =>
        err switch
        {
            EventCoordinatorError.DuplicateKey => "Duplicate key\n",
            EventCoordinatorError.KeyNotFound => "Key not found\n",
            EventCoordinatorError.InvalidKey => "Key is not valid, likely because it was empty\n",
            EventCoordinatorError.Unknown => "Unknown error occured\n",
            EventCoordinatorError => "Catch-all error, shouldn't happen\n"
        };
}
