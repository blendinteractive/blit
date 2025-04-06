using System.ComponentModel.DataAnnotations;

namespace ExampleSite.Models.Pages;

[ContentType(DisplayName = "Start Page",
    GroupName = "Specialized",
    GUID = "d080aa28-9336-4d4c-93a0-4570b42af2be")]
public class StartPage : PageData
{
    public virtual string? OldUrl { get; set; }
}
