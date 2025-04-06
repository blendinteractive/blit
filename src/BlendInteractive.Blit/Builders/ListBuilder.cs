namespace BlendInteractive.Blit.Builders;

public class ListBuilder<T>
{
    private readonly List<T> items = new List<T>();

    public ListBuilder<T> Add(T item)
    {
        items.Add(item);
        return this;
    }

    public ListBuilder<T> AddRange(IEnumerable<T> items)
    {
        this.items.AddRange(items);
        return this;
    }

    public ListBuilder<T> AddIf(bool shouldAdd, Func<T> generate)
    {
        if (!shouldAdd)
            return this;
        return Add(generate());
    }

    public IList<T> Done() => items;
}

public static class ListBuilder
{
    public static ListBuilder<T> Create<T>() => new ListBuilder<T>();
}