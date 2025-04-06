namespace BlendInteractive.Blit;

/// <summary>
/// Finds the "for this site" folder for a given site. If no site ID is provided, then it will fallback to the wildcard site.
/// </summary>
public record ForThisSiteLocator(IEnumerable<IFragment> SiteId) : ILocator;