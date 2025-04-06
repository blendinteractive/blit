using BlendInteractive.Blit.Builders;
using BlendInteractive.Blit.Builders.Typed;

namespace BlendInteractive.Blit;

public record ContentQuery(IEnumerable<ILocator> Locators) : IContentReference
{
    public static ContentQueryBuilder Build() => new ContentQueryBuilder();
    public static TypedContentQueryBuilder<T> Build<T>() => new TypedContentQueryBuilder<T>();
}