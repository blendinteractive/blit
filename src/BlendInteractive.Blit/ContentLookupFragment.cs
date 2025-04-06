namespace BlendInteractive.Blit;

public record ContentLookupFragment(ContentEmbedType EmbedType, ContentQuery Query, string FallbackUrl) : IFragment;
