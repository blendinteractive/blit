namespace BlendInteractive.Blit.Builders;

public abstract class ContentQueryBuilder<TSelf> : IContentQueryBuilder where TSelf : IContentQueryBuilder
{
    protected IList<ILocator> locators = new List<ILocator>();

    protected abstract TSelf Self { get; }

    protected abstract TSelf NewInstance();

    public TSelf Add(ILocator locator)
    {
        locators.Add(locator);
        return Self;
    }

    public TSelf Match(string name, IEnumerable<IFragment> fragments)
        => Add(new MatchTextLocator(name, fragments));
    public TSelf Match(string name, string value)
        => Match(name, new[] { new TextFragment(value) });

    public TSelf OfType(params IFragment[] fragments)
        => Add(new OfTypeLocator(fragments));
    public TSelf OfType(string type)
        => OfType(new IFragment[] { new TextFragment(type) });
    public TSelf OfType<T>()
        => OfType(typeof(T).GetShortTypeName());

    public TSelf Id(params IFragment[] fragments)
        => Add(new MatchIdLocator(fragments));
    public TSelf Id(string id)
        => Id(new IFragment[] { new TextFragment(id) });

    public TSelf Tree(TreeLocatorType type, Action<TSelf> query)
    {
        var builder = NewInstance();
        query(builder);
        return Add(new TreeLocator(type, new ContentQuery(builder.Done())));
    }

    public TSelf ChildOf(Action<TSelf> query)
        => Tree(TreeLocatorType.Child, query);

    public TSelf DescendantOf(Action<TSelf> query)
        => Tree(TreeLocatorType.Ancestor, query);

    public TSelf ForThisPage()
        => Add(new ForThisPageLocator());

    public TSelf ForThisSite(params IFragment[] fragments)
        => Add(new ForThisSiteLocator(fragments));
    public TSelf ForThisSite(string guid)
        => ForThisSite(new TextFragment(guid));
    public TSelf ForThisSite(Guid guid)
        => ForThisSite(guid.ToString());

    public IEnumerable<ILocator> Done() => locators;

    public ContentQuery AsQuery() => new ContentQuery(locators);

    public ContentLookupFragment AsFragment(ContentEmbedType embedType, string fallBackUrl)
        => new ContentLookupFragment(embedType, AsQuery(), fallBackUrl);
}

public class ContentQueryBuilder : ContentQueryBuilder<ContentQueryBuilder>
{
    protected override ContentQueryBuilder Self => this;

    protected override ContentQueryBuilder NewInstance() => new ContentQueryBuilder();
}
