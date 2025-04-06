namespace BlendInteractive.Blit.Optimizely;

public record ImportStatus(int FirstPassComplete, int SecondPassComplete, int TotalRecords)
{
    public override string ToString() => $"First pass: {FirstPassComplete}, Second pass: {SecondPassComplete}, Total: {TotalRecords}";
}