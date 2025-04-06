namespace ExampleSite.Models.Properties;

[ContentType(DisplayName = "SEO Meta Data",
    AvailableInEditMode = true,
    GUID = "df83924f-978c-43e6-ad15-81baaa3a0ee0")]
public class MetaDataProperty : BlockData
{
    public virtual string? MetaTitle { get; set; }
}
