using System.Xml.Linq;

namespace BlendInteractive.Blit.Xml;

public partial class XmlContentSerializer
{
    private static class ReaderUtility
    {
        public static Content ReadContent(XNode node)
        {
            if (node is not XElement element)
                throw new InvalidOperationException("Expecting an XElement");

            if (element.Name != "content")
                throw new InvalidOperationException("Expecting an XElement");

            var type = element.Attribute("type")?.Value ?? throw new InvalidOperationException("No valid type for Content node");
            var id = element.Attribute("id")?.Value ?? throw new InvalidOperationException("No valid id for Content node");

            var action = ContentActions.None;
            var actionsNode = element.Element("actions");
            if (actionsNode is not null)
            {
                foreach (var actionElement in actionsNode.Elements())
                {
                    action |= Enum.Parse<ContentActions>(actionElement.Name.LocalName);
                }
            }

            var queryNode = element.Element("query");
            if (queryNode is null)
                throw new InvalidOperationException("query node is required");

            var query = Query(queryNode);

            ContentQuery? parent = null;
            var parentNode = element.Element("parent");
            if (parentNode is not null)
            {
                parent = Query(parentNode);
            }

            IEnumerable<IProperty>? stageOne = null;
            var stageOneNode = element.Element("stageone");
            if (stageOneNode is not null)
                stageOne = Properties(stageOneNode);

            IEnumerable<IProperty>? stageTwo = null;
            var stageTwoNode = element.Element("stagetwo");
            if (stageTwoNode is not null)
                stageTwo = Properties(stageTwoNode);

            return new Content(
                id,
                type,
                action,
                query,
                parent,
                stageOne,
                stageTwo
            );
        }

        private static ContentQuery Query(XElement element)
        {
            var locators = element.Elements().Select(node => node.Name.LocalName switch
            {
                "text" => Text(node),
                "type" => OfType(node),
                "id" => Id(node),
                "tree" => Tree(node),
                "forthispage" => new ForThisPageLocator(),
                "forthissite" => ForThisSite(node),
                _ => throw new InvalidOperationException($"Uknown locator type {node.Name.LocalName}")
            });

            return new ContentQuery(locators.ToArray());
        }

        private static ILocator Text(XElement element)
        {
            var name = element.Attribute("name")?.Value;
            if (name is null)
                throw new InvalidOperationException("name is required on Text nodes");
            var fragments = Fragments(element);
            return new MatchTextLocator(name, fragments);
        }

        private static ILocator OfType(XElement element)
            => new OfTypeLocator(Fragments(element));

        private static ILocator Id(XElement element)
            => new MatchIdLocator(Fragments(element));

        private static ILocator ForThisSite(XElement element)
            => new ForThisSiteLocator(Fragments(element));


        private static ILocator Tree(XElement element)
        {
            var type = element.Attribute("type")?.Value;
            if (type is null)
                throw new InvalidOperationException("type is required on Tree nodes");

            var parsedType = Enum.Parse<TreeLocatorType>(type);

            var query = Query(element);
            return new TreeLocator(parsedType, query);
        }

        private static IEnumerable<IFragment> Fragments(XElement element)
        {
            var list = new List<IFragment>();

            foreach (var node in element.Nodes())
            {
                switch (node)
                {
                    case XCData cdata:
                        list.Add(new TextFragment(cdata.Value));
                        break;
                    case XText text:
                        list.Add(new TextFragment(text.Value));
                        break;
                    case XElement sub:
                        list.Add(FragmentFromElement(sub));
                        break;
                }
            }

            return list;
        }

        private static IFragment FragmentFromElement(XElement element)
            => element.Name.LocalName switch
            {
                "content" => ContentFragment(element),
                "lookup" => ContentLookupFragment(element),
                "var" => VariableFragment(element),
                "categorypath" => CategoryPathReferenceFragment(element),
                _ => throw new NotImplementedException($"Unrecognized element {element.Name.LocalName}")
            };

        private static IFragment ContentFragment(XElement element)
        {
            var embedTypeRaw = element.Attribute("embedtype")?.Value;
            if (embedTypeRaw == null)
                throw new InvalidOperationException("embedtype is required for embedded inline content fragments");

            var parsedEmbedType = Enum.Parse<ContentEmbedType>(embedTypeRaw);
            var content = ReadContent(element);

            return new InlineContentFragment(parsedEmbedType, content);
        }

        private static IFragment ContentLookupFragment(XElement element)
        {
            var fallback = element.Attribute("fallback")?.Value;
            if (fallback == null)
                throw new InvalidOperationException("fallback is required for content lookup fragments");
            var embedTypeRaw = element.Attribute("embedtype")?.Value;
            if (embedTypeRaw == null)
                throw new InvalidOperationException("embedtype is required for embedded inline content fragments");

            var parsedEmbedType = Enum.Parse<ContentEmbedType>(embedTypeRaw);

            var query = Query(element);
            return new ContentLookupFragment(parsedEmbedType, query, fallback);
        }

        private static IFragment VariableFragment(XElement element)
        {
            var variable = element.Attribute("name")?.Value;
            if (variable == null)
                throw new InvalidOperationException("name is required for variable fragments");
            return new VariableReference(variable);
        }

        public static IFragment CategoryPathReferenceFragment(XElement element)
        {
            var path = element.Elements()
                .Where(x => x.Name == "category")
                .Select(x => new CategoryName(Fragments(x)))
                .ToList();

            return new CategoryPathReference(path);
        }

        private static IEnumerable<IProperty> Properties(XElement element)
            => element.Elements().Select(Property);

        private static IProperty Property(XElement element)
            => element.Name.LocalName switch
            {
                "text" => TextProperty(element),
                "blob" => Blob(element),
                "nested" => Nested(element),
                "list" => List(element),
                _ => throw new NotImplementedException($"No deserializer for {element.Name.LocalName}")
            };

        private static IProperty TextProperty(XElement element)
        {
            var name = element.Attribute("name")?.Value;
            if (name == null)
                throw new InvalidOperationException("name is required on text properties");

            return new TextProperty(name, Fragments(element));
        }

        private static IProperty Blob(XElement element)
        {
            var name = element.Attribute("name")?.Value;
            if (name == null)
                throw new InvalidOperationException("name is required on blob properties");

            var extension = element.Attribute("extension")?.Value;
            if (extension == null)
                throw new InvalidOperationException("extension is required on blob properties");

            var data = Convert.FromBase64String(element.Value);

            return new BlobProperty(name, extension, data);
        }

        private static IProperty Nested(XElement element)
        {
            var name = element.Attribute("name")?.Value;
            if (name == null)
                throw new InvalidOperationException("name is required on nested properties");

            return new NestedProperty(name, Properties(element));
        }

        private static IProperty List(XElement element)
        {
            var name = element.Attribute("name")?.Value;
            if (name == null)
                throw new InvalidOperationException("name is required on list properties");

            var modeValue = element.Attribute("extension")?.Value;
            // var mode = Enum.TryParse(modeValue, out ListPropertyMode parsedMode) ? parsedMode : ListPropertyMode.ReplaceAllNodes;

            var items = element.Elements().Where(x => x.Name == "item").Select(x => new ListItem(Fragments(x))).ToList();
            return new ListProperty(name, /*mode, */items);
        }
    }
}
