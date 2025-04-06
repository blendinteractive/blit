namespace BlendInteractive.Blit.Optimizely;

public record BatchStatus(int Id, string FriendlyName, BatchState State, int Queued, int Processed, DateTime Date)
{
    public string Age => DateTime.UtcNow.Subtract(Date).ToString("h'h 'm'm'");
    public string ProgressPercent => Queued == 0 ? "N/A" : Math.Floor((float)Processed / Queued).ToString("0.0%");
    public string StateString => State switch
    {
        BatchState.Queued => "Queued",
        BatchState.InProgress => "In Progress",
        BatchState.Complete => "Completed",
        _ => throw new NotImplementedException($"No string conversion for {State}")
    };
}
