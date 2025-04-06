using EPiServer.Shell;

namespace BlendInteractive.Blit.Optimizely.UI.Models;

public abstract class BaseViewModel
{

    private string? basePath = null;

    public string Link(string? action)
    {
        if (basePath is null)
            basePath = Paths.ToResource(GetType(), "blit");

        if (string.IsNullOrEmpty(action))
            return basePath;

        return $"{basePath}/{action}";
    }
}
