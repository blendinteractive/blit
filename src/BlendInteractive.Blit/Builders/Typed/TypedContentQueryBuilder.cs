using System.Linq.Expressions;

namespace BlendInteractive.Blit.Builders.Typed;

public class TypedContentQueryBuilder<T> : ContentQueryBuilder<TypedContentQueryBuilder<T>>
{
    protected override TypedContentQueryBuilder<T> Self => this;

    protected override TypedContentQueryBuilder<T> NewInstance() => new TypedContentQueryBuilder<T>();

    public TypedContentQueryBuilder<T> Match<V>(Expression<Func<T, V>> expression, IEnumerable<IFragment> fragments)
        => Add(new MatchTextLocator(expression.GetMemberName(), fragments));
    public TypedContentQueryBuilder<T> Match<V>(Expression<Func<T, V>> expression, string value)
        => Match(expression, new[] { new TextFragment(value) });
}
