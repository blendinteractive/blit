namespace BlendInteractive.Blit;

public enum ImportStage
{
    /// <summary>
    /// The first pass of import, for most content that does not have
    /// dependencies
    /// </summary>
    StageOne,

    /// <summary>
    /// Second stage of import, for content that depends on other content
    /// (for example, a list of related items -- where related items must exist
    /// before they can be linked to)
    /// </summary>
    StageTwo,
}
