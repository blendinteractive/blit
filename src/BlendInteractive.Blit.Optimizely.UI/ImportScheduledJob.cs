using EPiServer.Scheduler;

namespace BlendInteractive.Blit.Optimizely.UI;

[ScheduledJob(DisplayName = "Blit Processing Job",
    GUID = "4c6ccf27-b134-4de8-b926-1cfa54cacf17",
    Description = "Runs any currently queued import jobs through the Blit import process")]
public class ImportScheduledJob : ScheduledJobBase
{
    private readonly ContentImportService _importService;
    private readonly BlitConfiguration _configuration;

    public ImportScheduledJob(ContentImportService importService, BlitConfiguration configuration)
    {
        _importService = importService;
        _configuration = configuration;
        IsStoppable = true;
    }

    public override string Execute()
    {
        try
        {
            bool batchProcessed = true;
            while (batchProcessed)
            {
                batchProcessed = _importService.ProcessBatch((status) =>
                {
                    this.OnStatusChanged(status.ToString());
                });
            }
        }
        catch (Exception ex)
        {
            var handler = _configuration.HandleException;
            if (handler != null)
            {
                var handled = handler(ex);
                if (handled == ErrorLoggingResult.DoNotRethrow)
                    return $"Exception: {ex.Message} - {ex.StackTrace}";
            }

            throw;
        }

        return "OK";
    }
}
