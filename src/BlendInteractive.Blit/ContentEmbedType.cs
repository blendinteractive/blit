namespace BlendInteractive.Blit;

public enum ContentEmbedType
{
    /// <summary>
    /// Renders the embedded content as an integer. Used for content areas and content reference types
    /// </summary>
    ID = 0,

    /// <summary>
    /// Renders the embedded content as a permanent URL. Used for links within the CMS.
    /// </summary>
    PermanentUrl,

    /// <summary>
    /// Renders as a block embedded in an XHtmlString. Used for embedding blocks in rich text areas.
    /// </summary>
    EmbeddedBlock
}
