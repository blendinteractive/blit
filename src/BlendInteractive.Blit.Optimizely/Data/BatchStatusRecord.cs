using System.Data;

namespace BlendInteractive.Blit.Optimizely.Data;

public record BatchStatusRecord(int Id, string Name, int StageId, DateTime Created, DateTime? Started, DateTime? Completed, int ItemsQueued, int ItemsProcessed)
{
    public static BatchStatusRecord FromDataReader(IDataReader reader)
        => new BatchStatusRecord(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetDateTime(3),
            reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            reader.GetInt32(6),
            reader.GetInt32(7)
        );

    public static BatchStatus AsBatchStatus(BatchStatusRecord record)
        => new BatchStatus(record.Id, record.Name, (BatchState)record.StageId, record.ItemsQueued, record.ItemsProcessed, record.Created);
}
