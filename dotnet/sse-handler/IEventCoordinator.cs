using System.Text;
using DotNext;

namespace SseHandler;

public interface IEventCoordinator
{
    Result<CancellationTokenSource, EventCoordinatorError> Add(string id, Stream stream);
    Task<Result<bool, EventCoordinatorError>> SendMessage(string id, object message);
    Result<bool, EventCoordinatorError> Remove(string id);
    void RemoveAll();
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
    public static string Stringify(this EventCoordinatorError err)
    {
        var stringBuilder = new StringBuilder();
        switch (err)
        {
            case EventCoordinatorError.DuplicateKey:
                stringBuilder.Append("Duplicate key");
                break;
            case EventCoordinatorError.KeyNotFound:
                stringBuilder.Append("Key not found");
                break;
            case EventCoordinatorError.InvalidKey:
                stringBuilder.Append("Key is not valid, likely because it was empty");
                break;
            case EventCoordinatorError.Unknown:
            default:
                stringBuilder.Append("Unknown error occured");
                break;
        }

        stringBuilder.AppendLine();
        return stringBuilder.ToString();
    }
}
