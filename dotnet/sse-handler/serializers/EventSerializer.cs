using System.Text;

namespace SseHandler.Serializers;

public interface IEventSerializer
{
    byte[] SerializeData(object message);
    byte[] SerializeEvent(object message);
}

public abstract class EventSerializer : IEventSerializer
{
    static string prepandData = "data: ";
    static string prepandEvent = "event: ";

    protected abstract string Serialize(object message);

    protected virtual byte[] Serialize(string prepand, object message)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append(prepand).Append(Serialize(message)).AppendLine().AppendLine();

        return Encoding.UTF8.GetBytes(stringBuilder.ToString());
    }

    public byte[] SerializeData(object message)
    {
        return Serialize(prepandData, message);
    }

    public byte[] SerializeEvent(object message)
    {
        return Serialize(prepandEvent, message);
    }
}
