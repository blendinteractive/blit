namespace BlendInteractive.Blit.Builders;

public abstract class AbstractPropertyBuilder<TSelf> : IPropertyListBuilder where TSelf : IPropertyListBuilder
{
    protected readonly IList<IProperty> properties = new List<IProperty>();

    protected abstract TSelf Self { get; }

    protected abstract TSelf CreateNew();

    protected TSelf Add(IProperty property)
    {
        properties.Add(property);
        return Self;
    }

    public TSelf Text(string name, params IFragment[] fragments)
        => Add(new TextProperty(name, fragments));
    public TSelf Text(string name, string simpleValue)
        => Text(name, new[] { new TextFragment(simpleValue) });


    public TSelf Blob(string name, string fileExtension, byte[] data)
        => Add(new BlobProperty(name, fileExtension, data));

    public TSelf Nested(string name, Action<TSelf> action)
    {
        var builder = CreateNew();
        action(builder);
        return Add(new NestedProperty(name, builder.Done()));
    }

    public TSelf List(string name, Action<ListPropertyBuilder> action)
    {
        var builder = new ListPropertyBuilder();
        action(builder);

        return Add(new ListProperty(name, builder.Items));
    }

    public class ListPropertyBuilder
    {
        public List<ListItem> Items { get; } = new List<ListItem>();
        public ListPropertyBuilder Add(params IFragment[] fragments)
        {
            Items.Add(new ListItem(fragments));
            return this;
        }
    }

    public IEnumerable<IProperty> Done() => properties;
}