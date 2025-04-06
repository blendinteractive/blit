using System.Linq.Expressions;

namespace BlendInteractive.Blit.Builders.Typed;

public class TypedPropertyBuilder<T> : AbstractPropertyBuilder<TypedPropertyBuilder<T>>
{
    protected override TypedPropertyBuilder<T> Self => this;

    protected override TypedPropertyBuilder<T> CreateNew() => new TypedPropertyBuilder<T>();

    public TypedPropertyBuilder<T> Text<V>(Expression<Func<T, V>> expression, params IFragment[] fragments)
        => Add(new TextProperty(expression.GetMemberName(), fragments));
    public TypedPropertyBuilder<T> Text<V>(Expression<Func<T, V>> expression, string simpleValue)
        => Text(expression, new[] { new TextFragment(simpleValue) });

    public TypedPropertyBuilder<T> Blob<V>(Expression<Func<T, V>> expression, string fileExtension, byte[] data)
        => Add(new BlobProperty(expression.GetMemberName(), fileExtension, data));

    public TypedPropertyBuilder<T> Nested<V>(Expression<Func<T, V>> expression, Action<TypedPropertyBuilder<V>> action)
    {
        var builder = new TypedPropertyBuilder<V>();
        action(builder);
        return Add(new NestedProperty(expression.GetMemberName(), builder.Done()));
    }

    public TypedPropertyBuilder<T> List<V>(Expression<Func<T, V>> expression, Action<ListPropertyBuilder> action)
    {
        var builder = new ListPropertyBuilder();
        action(builder);

        return Add(new ListProperty(expression.GetMemberName(), builder.Items));
    }
}
