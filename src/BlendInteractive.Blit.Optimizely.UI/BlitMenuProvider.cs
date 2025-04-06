using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace BlendInteractive.Blit.Optimizely.UI;

[MenuProvider]
public class BlitMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var url = Paths.ToResource(GetType(), "blit");

        var link = new UrlMenuItem("Blend Import Tool",
            MenuPaths.Global + "/blit",
            url)
        {
            SortIndex = 100
        };

        return new List<MenuItem> { link };
    }
}
