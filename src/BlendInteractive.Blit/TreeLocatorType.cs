namespace BlendInteractive.Blit;

public enum TreeLocatorType
{
    /// <summary>
    /// Node can be a child or descendant of ancester node
    /// </summary>
    Ancestor,

    /// <summary>
    /// Node must be an immediate child of the parent node
    /// </summary>
    Child
};