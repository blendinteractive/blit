using BlendInteractive.Blit.Builders;
using BlendInteractive.Blit.Builders.Typed;

namespace BlendInteractive.Blit;

public record Content(
    string Id,
    string Type,
    ContentActions Actions,
    ContentQuery Query,
    ContentQuery? Parent,
    IEnumerable<IProperty>? StageOne,
    IEnumerable<IProperty>? StageTwo
) : IContentReference
{
    public static ContentBuilder Build(string id, string type, ContentActions actions) => new ContentBuilder(id, type, actions);

    public static TypedContentBuilder<T> Build<T>(string id, ContentActions actions) => new TypedContentBuilder<T>(id, actions);
}