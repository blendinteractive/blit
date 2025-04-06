namespace BlendInteractive.Blit;

public static class TypeUtilities
{
    public static string GetShortTypeName(this Type type)
        => $"{type.FullName}, {type.Assembly.GetName().Name}";

}
