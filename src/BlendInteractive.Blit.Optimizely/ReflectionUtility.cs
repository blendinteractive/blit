using EPiServer.Core;
using EPiServer;

namespace BlendInteractive.Blit.Optimizely;

public static class ReflectionUtility
{
    public static IContent CreateContent(this IContentRepository contentRepository, Type contentType, ContentReference parentReference)
    {
        // Same as: var newContent = this.contentRepository.GetDefault<T>(parent.ContentLink);
        var method = typeof(IContentRepository).GetMethod("GetDefault", new[] { typeof(ContentReference) })!
            .MakeGenericMethod(contentType);
        var newContent = method.Invoke(contentRepository, new object[] { parentReference });
        if (newContent == null)
            throw new InvalidOperationException("Newly created content was null");
        return (IContent)newContent;
    }

    public static Type? GetPropertyType(this object content, string propertyName)
    {
        var property = content.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (property == null)
            return null;

        return property.PropertyType;
    }

    public static object? GetPropertyValue(this object content, string propertyName)
    {
        var property = content.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (property == null)
            return null;

        var getMethod = property.GetGetMethod();
        if (getMethod == null)
            throw new InvalidOperationException($"Property {propertyName} of {content.GetType().FullName} has no get method.");

        return getMethod.Invoke(content, new object[0]);
    }

    public static void SetPropertyValue(this object content, string propertyName, object? value)
    {
        var property = content.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (property == null)
            throw new InvalidOperationException($"Could not find property {propertyName} of {content.GetType().FullName}");

        var setMethod = property.GetSetMethod();
        if (setMethod == null)
            throw new InvalidOperationException($"Property {propertyName} of {content.GetType().FullName} has no set method.");

        setMethod.Invoke(content, new object?[] { value });
    }

}