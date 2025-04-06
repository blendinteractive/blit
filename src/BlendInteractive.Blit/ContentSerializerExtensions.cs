namespace BlendInteractive.Blit;

public static class ContentSerializerExtensions
{
    public static Content Deserialize(this IContentSerializer contentSerializer, string contentString)
    {
        using var reader = new StringReader(contentString);
        return contentSerializer.ReadFrom(reader);
    }

    public static string Serialize(this IContentSerializer contentSerializer, Content content)
    {
        using var writer = new StringWriter();
        contentSerializer.WriteTo(content, writer);
        return writer.ToString();
    }
}
