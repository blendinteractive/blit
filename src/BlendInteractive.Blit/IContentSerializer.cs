namespace BlendInteractive.Blit;

public interface IContentSerializer
{
    void WriteTo(Content content, TextWriter writer);

    Content ReadFrom(TextReader reader);
}
