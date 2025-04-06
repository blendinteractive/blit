using System.Xml.Linq;

namespace BlendInteractive.Blit.Xml;

public partial class XmlContentSerializer
{
    private static class XmlWriterUtility
    {
        public static XElement Content(Content content)
        {
            return new XElement("content", ContentInner(content));
        }

        private static object[] ContentInner(Content content)
        {
            var list = new List<object>();

            list.Add(new XAttribute("id", content.Id));
            list.Add(new XAttribute("type", content.Type));

            list.Add(new XElement("actions",
                Enum.GetValues<ContentActions>()
                    .Where(x => x != ContentActions.None && content.Actions.HasFlag(x))
                    .Select(x => new XElement(x.ToString()))
                    .ToArray()
            ));

            list.Add(new XElement("query", ContentQueryInner(content.Query)));

            if (content.Parent != null)
                list.Add(new XElement("parent", ContentQueryInner(content.Parent)));

            if (content.StageOne != null)
                list.Add(new XElement("stageone", Properties(content.StageOne)));

            if (content.StageTwo != null)
                list.Add(new XElement("stagetwo", Properties(content.StageTwo)));

            return list.ToArray();
        }

        private static object[] ContentQueryInner(ContentQuery query)
            => query.Locators.Select(x => x switch {
                MatchTextLocator text => new XElement("text",
                    new object[]
                    {
                            new XAttribute("name", text.Name)
                    }
                    .Concat(Fragments(text.Value))
                    .ToArray()
                ),
                OfTypeLocator ofType => new XElement("type",
                    Fragments(ofType.Type)
                ),
                MatchIdLocator id => new XElement("id",
                    Fragments(id.Id)
                ),
                TreeLocator tree => new XElement("tree",
                    new object[]
                    {
                            new XAttribute("type", tree.Type.ToString())
                    }
                    .Concat(ContentQueryInner(tree.Query))
                    .ToArray()
                ),
                ForThisPageLocator ftp => new XElement("forthispage"),
                ForThisSiteLocator fts => new XElement("forthissite",
                    Fragments(fts.SiteId)
                ),
                _ => throw new NotImplementedException($"No serializer for {x.GetType().FullName}")
            }).ToArray();

        private static XElement ContentReference(IContentReference contentReference)
            => contentReference switch
            {
                ContentQuery query => new XElement("ref", ContentQueryInner(query)),
                Content content => Content(content),
                _ => throw new NotImplementedException($"No serializer for {contentReference.GetType().FullName}")
            };

        private static object[] Fragments(IEnumerable<IFragment> fragments)
            => fragments.Select<IFragment, XNode>(x => x switch
            {
                TextFragment text => text.Text.IndexOf('<') >= 0 ? new XCData(text.Text) : new XText(text.Text),
                InlineContentFragment content => new XElement("content",
                    new object[]
                    {
                            new XAttribute("embedtype", content.EmbedType.ToString())
                    }
                    .Concat(ContentInner(content.Content))
                    .ToArray()
                ), // (XNode)ContentReference(content.Content),
                ContentLookupFragment lookup => new XElement("lookup",
                    new object[]
                    {
                            new XAttribute("fallback", lookup.FallbackUrl),
                            new XAttribute("embedtype", lookup.EmbedType.ToString())
                    }
                    .Concat(ContentQueryInner(lookup.Query))
                    .ToArray()
                ),
                VariableReference variable => new XElement("var",
                    new XAttribute("name", variable.Name)
                ),
                CategoryPathReference category => new XElement("categorypath",
                    category.CategoryPath.Select(x => (object)new XElement("category", Fragments(x.Fragments))).ToArray()
                ),
                _ => throw new NotImplementedException($"No serializer for {x.GetType().FullName}")
            }).ToArray();

        private static object[] Properties(IEnumerable<IProperty> properties)
            => properties.Select(x => x switch
            {
                TextProperty text => new XElement("text",
                    new object[]
                    {
                            new XAttribute("name", text.Name),
                    }
                    .Concat(Fragments(text.Fragments))
                    .ToArray()
                ),
                BlobProperty blob => new XElement("blob",
                    new XAttribute("name", blob.Name),
                    new XAttribute("extension", blob.FileExtension),
                    new XText(Convert.ToBase64String(blob.Data))
                ),
                NestedProperty nested => new XElement("nested",
                    new object[]
                    {
                            new XAttribute("name", nested.Name),
                    }
                    .Concat(Properties(nested.Properties))
                    .ToArray()
                ),
                ListProperty list => new XElement("list",
                    new object[]
                    {
                            new XAttribute("name", list.Name),
                        // new XAttribute("mode", list.Mode.ToString()),
                    }
                    .Concat(list.Items.Select(x => new XElement("item", Fragments(x.Fragments))))
                    .ToArray()
                ),
                _ => throw new NotImplementedException($"No serializer for {x.GetType().FullName}")
            }).ToArray();
    }
}
