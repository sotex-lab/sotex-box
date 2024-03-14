using System.Text;
using DotNext;

namespace SseHandler;

public interface IEventCoordinator
{
    Result<CancellationTokenSource, EventCoordinatorError> Add(string id, Stream stream);
    Task<Result<bool, EventCoordinatorError>> SendMessage(string id, object message);
    Result<bool, EventCoordinatorError> Remove(string id);
    void RemoveAll();
    IEnumerable<string> GetConnectionIds();
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
