using System.Xml.Linq;

namespace BlendInteractive.Blit.Builders;

public static class XElementBuilder
{
    public static XElement Build(string name, Action<ListBuilder<XObject>> action)
    {
        var builder = new ListBuilder<XObject>();
        action(builder);
        return new XElement(name, builder.Done().ToArray());
    }

    public static ListBuilder<XObject> Attribute(this ListBuilder<XObject> builder, string name, string value)
        => builder.Add(new XAttribute(name, value));

    public static ListBuilder<XObject> Element(this ListBuilder<XObject> builder, string name, Action<ListBuilder<XObject>> action)
        => builder.Add(XElementBuilder.Build(name, action));

    public static ListBuilder<XObject> Element(this ListBuilder<XObject> builder, string name, object[] content)
        => builder.Add(new XElement(name, content));
}
