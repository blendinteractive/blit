using EPiServer.Core;
using System.Xml.Serialization;

namespace BlendInteractive.Blit.Optimizely;

public interface IContentQueryResolver
{
    IContent? FindContent(ImportContext context, ContentQuery query);

    ContentReference? FindReference(ImportContext context, ContentQuery query);
}