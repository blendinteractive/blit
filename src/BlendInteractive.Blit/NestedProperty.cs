namespace BlendInteractive.Blit;

public record NestedProperty(string Name, IEnumerable<IProperty> Properties) : IProperty;
