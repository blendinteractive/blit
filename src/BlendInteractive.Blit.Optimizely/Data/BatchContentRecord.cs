using System.Data;

namespace BlendInteractive.Blit.Optimizely.Data;

public record BatchContentRecord(int Id, int BatchId, int Priority, string? ContentPath, string? Content, int StageId, DateTime Created, DateTime? Started, DateTime? Completed)
{
    public static BatchContentRecord FromDataReader(IDataReader reader) => new BatchContentRecord(
        reader.GetInt32(0),
        reader.GetInt32(1),
        reader.GetInt32(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.GetInt32(5),
        reader.GetDateTime(6),
        reader.IsDBNull(7) ? null : reader.GetDateTime(7),
        reader.IsDBNull(8) ? null : reader.GetDateTime(8)
    );
}
