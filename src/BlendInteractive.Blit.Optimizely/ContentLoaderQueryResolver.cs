using EPiServer.Core;
using EPiServer.Web;
using EPiServer;
using System.Text;
using System.Xml.Serialization;

namespace BlendInteractive.Blit.Optimizely;

public class ContentLoaderQueryResolver : IContentQueryResolver
{
    private readonly IContentLoader contentLoader;
    private readonly ContentAssetHelper contentAssetHelper;
    private readonly ISiteDefinitionRepository siteDefinitionRepository;

    public ContentLoaderQueryResolver(IContentLoader contentLoader, ContentAssetHelper contentAssetHelper, ISiteDefinitionRepository siteDefinitionRepository)
    {
        this.contentLoader = contentLoader;
        this.contentAssetHelper = contentAssetHelper;
        this.siteDefinitionRepository = siteDefinitionRepository;
    }

    public ContentReference? FindReference(ImportContext context, ContentQuery query)
    {
        var byId = query.Locators.OfType<MatchIdLocator>().FirstOrDefault();
        if (byId != null)
        {
            return new ContentReference(ResolveFragments(context, byId.Id));
        }

        var foundContent = FindContent(context, query);
        return foundContent?.ContentLink;
    }

    public IContent? FindContent(ImportContext context, ContentQuery contentQuery)
    {
        // Skip search if we have the ID.
        var byId = contentQuery.Locators.OfType<MatchIdLocator>().FirstOrDefault();
        if (byId != null)
        {
            var contentId = ResolveFragments(context, byId.Id);
            return contentLoader.Get<IContent>(new ContentReference(contentId));
        }

        var forThisPage = contentQuery.Locators.OfType<ForThisPageLocator>().FirstOrDefault();
        if (forThisPage != null)
        {
            var currentPage = context.CurrentPage;
            if (currentPage == null)
                throw new InvalidOperationException("No page to create for-this-page content");

            var forThisPageReference = contentAssetHelper.GetOrCreateAssetFolder(currentPage.ContentLink);
            return forThisPageReference;
        }

        var forThisSite = contentQuery.Locators.OfType<ForThisSiteLocator>().FirstOrDefault();
        if (forThisSite != null)
        {

            var siteId = ResolveFragments(context, forThisSite.SiteId);
            SiteDefinition? currentSite;
            if (string.IsNullOrEmpty(siteId))
            {
                currentSite = siteDefinitionRepository.List().FirstOrDefault(x => x.Hosts.Any(y => y.IsWildcardHost()));
            }
            else
            {
                Guid guid = Guid.Parse(siteId);
                currentSite = siteDefinitionRepository.Get(guid);
            }


            if (currentSite == null)
                throw new InvalidOperationException("Could not locate site");

            var root = !ContentReference.IsNullOrEmpty(currentSite.SiteAssetsRoot) ? currentSite.SiteAssetsRoot : currentSite.GlobalAssetsRoot;

            var folder = contentLoader.Get<IContent>(root);
            return folder;
        }

        // Otherwise, figure out which content actually needs to be searched.
        IEnumerable<IContent> query;
        var treeLocator = contentQuery.Locators.OfType<TreeLocator>().FirstOrDefault();

        if (treeLocator != null)
        {
            var parent = FindContent(context, treeLocator.Query);
            if (parent == null)
                return null;

            query = treeLocator.Type switch
            {
                TreeLocatorType.Child => contentLoader.GetChildren<IContent>(parent.ContentLink),
                TreeLocatorType.Ancestor => AllContent(parent.ContentLink),
                _ => throw new NotImplementedException($"Unknown tree locator type {treeLocator.Type}")
            };
        }
        else
        {
            query = AllContent(SiteDefinition.Current.RootPage);
        }

        // Apply additional filters
        foreach (var term in contentQuery.Locators)
        {
            switch (term)
            {
                case OfTypeLocator ofType:
                    var type = Type.GetType(ResolveFragments(context, ofType.Type));
                    if (type == null)
                        throw new InvalidOperationException($"Could not locate by type {ofType.Type}");
                    query = query.Where(x => type.IsAssignableFrom(x.GetOriginalType()));
                    break;
                case MatchTextLocator match:
                    var propName = match.Name;
                    query = query.Where(x =>
                    {
                        var propValue = x.GetPropertyValue(propName)?.ToString();
                        var matchValue = ResolveFragments(context, match.Value);
                        return propValue != null && string.Compare(propValue, matchValue, true) == 0;
                    });
                    break;
            }
        }

        // Execute query
        return query.FirstOrDefault();
    }

    private string ResolveFragments(ImportContext context, IEnumerable<IFragment> fragments)
    {
        var buffer = new StringBuilder();
        foreach (var fragment in fragments)
        {
            switch (fragment)
            {
                case TextFragment text:
                    buffer.Append(text.Text);
                    break;
                case VariableReference variable:
                    var value = context.GetVariableValue(variable.Name);
                    if (value == null)
                        throw new InvalidOperationException($"Variable does not exist {variable.Name}");
                    buffer.Append(value);
                    break;
                case InlineContentFragment _:
                    throw new InvalidOperationException("Can't use inline content in content queries");
                case ContentLookupFragment _: // TODO: resolve this to some kind of URL?
                    throw new InvalidOperationException("Can't use content lookup in content queries");
                case CategoryPathReference _:
                    throw new InvalidOperationException("Can't use category path in content queries");
                default:
                    throw new NotImplementedException($"No resolver for {fragment.GetType().FullName}");
            }
        }
        return buffer.ToString();
    }

    private IEnumerable<IContent> AllContent(ContentReference root)
    {
        var rootContent = contentLoader.Get<IContent>(root);
        Stack<IContent> stack = new Stack<IContent>();
        stack.Push(rootContent);

        while (stack.Any())
        {
            var content = stack.Pop();
            yield return content;

            var children = contentLoader.GetChildren<IContent>(content.ContentLink);
            foreach (var kid in children)
                stack.Push(kid);
        }
    }
}