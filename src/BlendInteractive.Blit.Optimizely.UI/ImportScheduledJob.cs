using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace BlendInteractive.Blit.Optimizely.UI;

[ScheduledPlugIn(DisplayName = "Blit Processing Job",
    GUID = "4c6ccf27-b134-4de8-b926-1cfa54cacf17",
    Description = "Runs any currently queued import jobs through the Blit import process")]
public class ImportScheduledJob : ScheduledJobBase
{
    public Injected<ContentImportService> ImportService;
    public Injected<BlitConfiguration> Configuration;

    public ImportScheduledJob()
    {
        IsStoppable = true;
    }

    public override string Execute()
    {
        try
        {
            bool batchProcessed = true;
            while (batchProcessed)
            {
                batchProcessed = ImportService.Service.ProcessBatch((status) =>
                {
                    this.OnStatusChanged(status.ToString());
                });
            }
        }
        catch (Exception ex)
        {
            var handler = Configuration.Service?.HandleException;
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