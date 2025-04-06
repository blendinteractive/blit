namespace BlendInteractive.Blit;

[Flags]
public enum ContentActions
{
    /// <summary>
    /// Do nothing with this content
    /// </summary>
    None = 0,

    /// <summary>
    /// Create this content if it does not exist
    /// </summary>
    Create = 1,

    /// <summary>
    /// Update this content if it does exist
    /// </summary>
    Update = 1 << 1,

    /// <summary>
    /// Move this content if it's not where it should be found
    /// </summary>
    Move = 1 << 2,

    /// <summary>
    /// Delete this content if its found
    /// </summary>
    Delete = 1 << 3,
}
