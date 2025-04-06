namespace BlendInteractive.Blit;

public record CategoryPathReference(IEnumerable<CategoryName> CategoryPath) : IFragment
{
    public static CategoryPathReference Create(params string[] path)
    {
        return new CategoryPathReference(path.Select(x => new CategoryName(new[] { new TextFragment(x) })).ToArray());
    }
}
