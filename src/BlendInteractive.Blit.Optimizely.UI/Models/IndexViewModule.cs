namespace BlendInteractive.Blit.Optimizely.UI.Models;

public class IndexViewModule(IEnumerable<BatchStatus> batches) : BaseViewModel
{
    public IEnumerable<BatchStatus> Batches { get; } = batches;
}
