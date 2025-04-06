using EPiServer.Shell.ObjectEditing;
using ExampleSite.Models.Properties;

namespace ExampleSite.Models.Pages;

[ContentType(DisplayName = "Article Page",
    GroupName = "General",
    GUID = "e1977208-a471-46af-bc8f-e25b8f456670")]
public class ArticlePage : PageData
{
    public virtual MetaDataProperty? MetaData { get; set; }

    public virtual string? OldUrl { get; set; }

    public virtual string? Title { get; set; }

    public virtual XhtmlString? Blurb { get; set; }

    public virtual DateTime? ArticleDate { get; set; }

    public virtual XhtmlString? Body { get; set; }

    // Some properties just to test that properties are working
    public virtual ContentReference? ExampleReference { get; set; }

    public virtual Url? ExampleUrlProperty { get; set; }

    public virtual ContentArea? MainContent { get; set; }

    [SelectOne(SelectionFactoryType = typeof(Blend.Optimizely.EditorDescriptors.EnumSelectionFactory<ExampleEnum>))]
    public virtual ExampleEnum ExampleEnum { get; set; }

    public virtual int? ExampleInteger { get; set; }
}

public enum ExampleEnum
{
    Default = 0,
    ValueOne = 1,
    ValueTwo = 2,
}