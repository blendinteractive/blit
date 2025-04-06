
namespace BlendInteractive.Blit.Optimizely.UI.Models;

public class ViewBatchViewModel : BaseViewModel
{
    public int BatchId { get; set; }
    public BatchStatus? Details { get; internal set; }
    public string? Log { get; internal set; }
}
