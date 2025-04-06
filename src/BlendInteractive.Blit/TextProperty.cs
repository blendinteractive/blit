namespace BlendInteractive.Blit;

public record TextProperty(string Name, IEnumerable<IFragment> Fragments) : IProperty;