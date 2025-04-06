using BlendInteractive.Datastore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BlendInteractive.Blit.Optimizely.Data;

public class Datastore : AbstractDatastore
{
    public Datastore(SqlConnection connection, SqlTransaction? transaction) : base(connection, transaction)
    {
    }

    public IEnumerable<VariableRecord> ListGlobalVariables()
        => Query($"SELECT Id, Name, Value FROM BlitGlobalVariable ORDER BY Name", VariableRecord.FromDataReader);

    public IEnumerable<VariableRecord> ListBatchVariables(int batchId)
        => Query($"SELECT Id, Name, Value FROM BlitBatchVariable WHERE BatchID = {batchId} ORDER BY Name", VariableRecord.FromDataReader);

    public IEnumerable<BatchContentRecord> ListBatchContent(int batchId)
        => Query($"SELECT Id, BatchId, Priority, ContentPath, Content, StageId, Created, Started, Completed FROM BlitContent WHERE BatchId = {batchId} ORDER BY Priority", BatchContentRecord.FromDataReader);

    public IEnumerable<BatchStatusRecord> ListBatches()
        => Query($@"
SELECT b.[Id], b.[Name], b.[StageId], b.[Created], b.[Started], b.[Completed],
    (SELECT Count(c.Id) FROM BlitContent c WHERE c.BatchId = b.Id and StageId = 1) as ItemsQueued,
    (SELECT Count(c.Id) FROM BlitContent c WHERE c.BatchId = b.Id and StageId = 3) as ItemsProcessed
FROM [BlitBatch] b
ORDER BY b.Created", BatchStatusRecord.FromDataReader);

    public void ReqeueBatch(int id)
        => ExecuteNonQuery($@"
UPDATE BlitContent SET StageId = {(int)BatchState.Queued} WHERE BatchId = {id};
UPDATE BlitBatch SET StageId = {(int)BatchState.Queued} WHERE Id = {id};
DELETE FROM BlitBatchLogEntry WHERE BatchId = {id};");

    public BatchStatusRecord? GetBatch(int id)
    {
        var records = Query($@"SELECT b.[Id], b.[Name], b.[StageId], b.[Created], b.[Started], b.[Completed],
    (SELECT Count(c.Id) FROM BlitContent c WHERE c.BatchId = b.Id and StageId = 1) as ItemsQueued,
    (SELECT Count(c.Id) FROM BlitContent c WHERE c.BatchId = b.Id and StageId = 3) as ItemsProcessed
FROM [BlitBatch] b
WHERE b.Id = {id}", BatchStatusRecord.FromDataReader);

        return records.FirstOrDefault();
    }



    public void DeleteAllGlobalVariables()
        => ExecuteNonQuery($"TRUNCATE TABLE BlitGlobalVariable");

    public void InsertVariable(Variable variable)
        => ExecuteNonQuery($"INSERT INTO BlitGlobalVariable (Name, Value) VALUES ({variable.Name}, {variable.Value})");

    public int CreateNewBatch(string friendlyName)
        => (int)ExecuteScalar($"INSERT INTO BlitBatch (Name, StageId, Created) OUTPUT Inserted.Id VALUES ({friendlyName}, {BatchState.Queued}, {DateTime.UtcNow})");

    public void InsertBatchVariable(int batchId, Variable variable)
        => ExecuteNonQuery($"INSERT INTO BlitBatchVariable (BatchID, Name, Value) VALUES ({batchId}, {variable.Name}, {variable.Value})");

    public void InsertBatchContent(int batchId, int priority, string? serializedContent, string? contentPath)
        => ExecuteNonQuery($"INSERT INTO BlitContent (BatchId, Priority, Content, ContentPath, StageId, Created) VALUES ({batchId}, {priority}, {serializedContent ?? (object)DBNull.Value}, {contentPath ?? (object)DBNull.Value}, {BatchState.Queued}, {DateTime.UtcNow})");

    internal void DeleteBatchLog(int batchId)
        => ExecuteNonQuery($"DELETE FROM BlitBatchLogEntry WHERE BatchId = {batchId}", cmd => cmd.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds);

    internal void DeleteBatchContent(int batchId)
        => ExecuteNonQuery($"DELETE FROM BlitContent WHERE BatchId = {batchId}", cmd => cmd.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds);

    internal void DeleteBatchVariables(int batchId)
        => ExecuteNonQuery($"DELETE FROM BlitBatchVariable WHERE BatchId = {batchId}", cmd => cmd.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds);

    internal void InsertNewLogEntry(int batchId, int? contentId, string message)
        => ExecuteNonQuery($"INSERT INTO [BlitBatchLogEntry] ([BatchId], [ContentId], [Date], [Text]) VALUES ({batchId}, {contentId}, {DateTime.UtcNow}, {message})");

    internal void DeleteBatch(int batchId)
        => ExecuteNonQuery($"DELETE FROM BlitBatch WHERE Id = {batchId}", cmd => cmd.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds);

    internal void StartBatch(int id)
        => ExecuteNonQuery($"UPDATE BlitBatch SET StageId = {BatchState.InProgress}, Started = {DateTime.UtcNow} WHERE Id = {id}");

    internal void CompleteBatch(int id)
        => ExecuteNonQuery($"UPDATE BlitBatch SET StageId = {BatchState.Complete}, Completed = {DateTime.UtcNow} WHERE Id = {id}");

    public void StartBatchContent(int id)
        => ExecuteNonQuery($"UPDATE BlitContent SET StageId = {(int)BatchState.InProgress}, Started = {DateTime.UtcNow} WHERE Id = {id}");

    public void CompleteBatchContent(int id)
        => ExecuteNonQuery($"UPDATE BlitContent SET StageId = {(int)BatchState.Complete}, Completed = {DateTime.UtcNow} WHERE Id = {id}");

    public IEnumerable<string> GetLog(int batchId)
        => Query($"SELECT [Text] FROM BlitBatchLogEntry WHERE BatchId = {batchId} ORDER BY [Date] ASC", (reader) => reader.GetString(0));


    internal IEnumerable<int> GetTopBatchId()
        => Query($"SELECT TOP 1 Id FROM BlitBatch ORDER BY Id DESC", reader => reader.GetInt32(0));

    internal IEnumerable<int> GetTopLogEntryIds(int id)
        => Query($"SELECT TOP 10 Id FROM BlitBatchLogEntry WHERE BatchId = {id} ORDER BY Id DESC", reader => reader.GetInt32(0));

    internal void DeleteLogEntry(int id)
        => ExecuteNonQuery($"DELETE FROM BlitBatchLogEntry WHERE Id = {id}");

    internal IEnumerable<int> GetTopContentIds(int id)
        => Query($"SELECT TOP 10 Id FROM BlitContent WHERE BatchId = {id} ORDER BY Id DESC", reader => reader.GetInt32(0));

    internal void DeleteContent(int id)
        => ExecuteNonQuery($"DELETE FROM BlitContent WHERE Id = {id}");
}