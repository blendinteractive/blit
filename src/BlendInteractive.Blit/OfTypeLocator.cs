namespace BlendInteractive.Blit;

/// <summary>
/// Matches a content's Type exactly
/// </summary>
public record OfTypeLocator(IEnumerable<IFragment> Type) : ILocator;