namespace BlendInteractive.Blit;

public record ListProperty(string Name, IEnumerable<ListItem> Items) : IProperty;