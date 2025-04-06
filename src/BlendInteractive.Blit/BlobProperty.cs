namespace BlendInteractive.Blit;

public record BlobProperty(string Name, string FileExtension, byte[] Data) : IProperty;