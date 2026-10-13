using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Xml;

public class XmlNodeUniversalConverter : JsonConverter<XmlNode>
{
    public override void WriteJson(JsonWriter writer, XmlNode value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        XmlDocument tempDoc = new XmlDocument();
        XmlNode nodeToSerialize;

        if (value is XmlDocument xmlDoc)
        {
            nodeToSerialize = xmlDoc;
        }
        else
        {
            // Wrap in a new XmlDocument to ensure it's a complete structure
            nodeToSerialize = tempDoc.ImportNode(value, true);
            tempDoc.AppendChild(nodeToSerialize);
            nodeToSerialize = tempDoc;
        }

        var converter = new XmlNodeConverter();
        converter.WriteJson(writer, nodeToSerialize, serializer);
    }

    public override XmlNode ReadJson(JsonReader reader, Type objectType, XmlNode existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var converter = new XmlNodeConverter();
        var doc = (XmlDocument)converter.ReadJson(reader, typeof(XmlDocument), null, serializer);

        if (doc == null)
            return null;

        // If target is XmlElement or XmlNode, return DocumentElement
        if (objectType == typeof(XmlElement) || objectType == typeof(XmlNode))
            return (XmlNode)(doc.DocumentElement ?? (XmlNode)doc);

        return doc;
    }
}
