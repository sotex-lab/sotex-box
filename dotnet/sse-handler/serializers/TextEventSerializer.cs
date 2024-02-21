namespace SseHandler.Serializers;

public class TextEventSerializer : EventSerializer
{
    protected override string Serialize(object message)
    {
        return message.ToString()!;
    }
}
