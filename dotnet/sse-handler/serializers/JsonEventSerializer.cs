using System.Text.Json;
using System.Text.Json.Serialization;

namespace SseHandler.Serializers;

public class JsonEventSerializer : EventSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonEventSerializer()
        : this(new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles, }) { }

    public JsonEventSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    protected override string Serialize(object message)
    {
        return JsonSerializer.Serialize(message, _options);
    }
}
