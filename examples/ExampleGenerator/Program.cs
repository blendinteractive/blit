
// Generating example content for Blit.

using BlendInteractive.Blit;
using BlendInteractive.Blit.Xml;
using ExampleSite.Models.Pages;

var blogPostPage = Content
    // Create or update a "ArticlePage" page type, using "blog-2024-01-05" as an identifier.
    .Build<ArticlePage>("blog-2024-01-05", ContentActions.Create | ContentActions.Update)

    // To see if a page already exists, query for a page that meets this criteria:
    // * The `OldUrl` matches "/articles/blog-2024-01-05"
    // * Is an `ArticlePage` content type
    .Query(q => q
        .Match(x => x.OldUrl, "/articles/blog-2024-01-05")
        .OfType<ArticlePage>()
    )

    // If a page needs to be created, query for a parent page that meets this criteria:
    // * An `ArticlesLandingPage` content type
    // * Is an immediate child of a page with an ID matching the `homepage-id` variable
    .ParentQuery(q => q
        .OfType<ArticlesLandingPage>()
        .Tree(TreeLocatorType.Child, t => t.Id(new VariableReference("homepage-id")))
    )

    // Set "stage-one" properties which are all the properties necessary for the 
    // page to exist, but none of the properties that may have dependencies on 
    // other content in the import.
    .StageOneProperties(p =>
    {
        // Make sure to set OldUrl since that's part of how we're uniquely indentifying imported pages.
        p.Text(x => x.OldUrl, "/articles/blog-2024-01-05");

        // Name is always required
        p.Text(x => x.Name, "Astronauts are just Star Sailors");

        // Some other text can be set now.
        p.Text(x => x.Title, "Astronauts are just Star Sailors");
        p.Text(x => x.Blurb, "<p>Astron is star; nautes is sailor.</p>");

        p.List(x => x.Category, cats =>
        {
            cats.Add(CategoryPathReference.Create("Sections", "News Article", "Best Articles"));
        });

    })

    
    .StageTwoProperties(p =>
    {
        // Stage Two is for properties that may reference other imported content.
        // If there are no such properties, you can skip stage two with 
        // `.SkipStageTwo()` instead.

        // Create HTML with an embedded link
        p.Text(x => x.Body, new IFragment[] {
            // Start of the HTML fragment.
            new TextFragment("<p>Learn more about <a href=\""),

            // Replace the link with a permanent link if it exists.
            // Will search for an ancestor of the homepage-id value,
            // with "/etymology-facts" as the OldUrl. If found, the 
            // internal permanent URL will be embedded here. If not,
            // the fallback of "/etymology-facts" will be used.
            new ContentLookupFragment(
                ContentEmbedType.PermanentUrl,
                new ContentQuery(ContentQuery.Build()
                    .Tree(TreeLocatorType.Ancestor, t => t.Id(new VariableReference("homepage-id")))
                    .Match("OldUrl", "/etymology-facts")
                    .Done()),
                "/etymology-facts"
            ),

            // Finish the link
            new TextFragment("\">Etymology Facts!</a></p>")
        });
    });

IContentSerializer serializer = new XmlContentSerializer();

var xml = serializer.Serialize(blogPostPage);

// Each piece of content is written out as individual XML files.
File.WriteAllText("./blog-2024-01-05.xml", xml);

// Then an index file is written with references to each XML file,
// in the order they should be processed.
File.WriteAllText("./index.txt", "./blog-2024-01-05.xml");

// These local files are good for local testing, but for use on a webserver,
// they are typically moved to something like S3 or Azure Storage (or at least
// someplace where they can be accessible via HTTP or HTTPS). The index file 
// is then rewritten to reference the files via their URLs. Then the index 
// file URL can be queued for processing.
//
// Once queued, kick off the scheduled job!
