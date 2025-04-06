using EPiServer.Framework.DataAnnotations;

namespace ExampleSite.Models.Media;

[ContentType(GUID = "837dd401-4612-48d4-b654-f7598635712c")]
[MediaDescriptor(ExtensionString = "jpg,jpeg,png,gif")]
public class ImageFile : ImageData
{
    public virtual string? AltText { get; set; }

    public virtual string? OldUrl { get; set; }
}
