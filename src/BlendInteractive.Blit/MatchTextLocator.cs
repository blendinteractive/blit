namespace BlendInteractive.Blit;

/// <summary>
/// Matches a property of a piece of content
/// </summary>
public record MatchTextLocator(string Name, IEnumerable<IFragment> Value) : ILocator;
