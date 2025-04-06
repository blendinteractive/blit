namespace BlendInteractive.Blit;

/// <summary>
/// Item is either a child or ancester of some other piece of content.
/// </summary>
public record TreeLocator(TreeLocatorType Type, ContentQuery Query) : ILocator;