using System.Xml.Linq;
using System.Xml;

namespace BlendInteractive.Blit.Xml;

public partial class XmlContentSerializer : IContentSerializer
{
    public Content ReadFrom(TextReader reader)
    {
        // var xmlReader = XmlReader.Create(reader);
        string data = reader.ReadToEnd();
        var xDoc = XDocument.Parse(data);// XNode.ReadFrom(xmlReader);
        var xRoot = xDoc.Root;
        if (xRoot == null)
            throw new InvalidOperationException("Failed to parse XML");

        var content = ReaderUtility.ReadContent(xRoot);
        return content;
    }

    public void WriteTo(Content content, TextWriter writer)
    {
        var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true });
        var root = XmlWriterUtility.Content(content);
        root.WriteTo(xmlWriter);
        xmlWriter.Flush();
    }
}