using System.Xml;
using System.Xml.Serialization;

namespace SseHandler.Serializers;

public class XmlEventSerializer : EventSerializer
{
    protected override string Serialize(object message)
    {
        var serializer = new XmlSerializer(message.GetType());
        using var stringStream = new StringWriter();
        using var xmlWriter = new XmlTextWriter(stringStream);
        serializer.Serialize(xmlWriter, message);
        return stringStream.ToString();
    }
}
