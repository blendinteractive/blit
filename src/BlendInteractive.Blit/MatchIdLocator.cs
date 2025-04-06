namespace BlendInteractive.Blit;

/// <summary>
/// Matches a content's ID directly
/// </summary>
public record MatchIdLocator(IEnumerable<IFragment> Id) : ILocator;