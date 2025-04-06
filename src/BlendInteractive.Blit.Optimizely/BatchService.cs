using BlendInteractive.Blit.Optimizely.Data;
using System.Net;
using System.Text;

namespace BlendInteractive.Blit.Optimizely;

public class BatchService : IBatchService
{
    private readonly DatastoreFactory datastore;
    private readonly IContentSerializer contentSerializer;

    public BatchService(DatastoreFactory datastore, IContentSerializer contentSerializer)
    {
        this.datastore = datastore;
        this.contentSerializer = contentSerializer;
    }

    private string GetTextFromPath(string path)
    {
        if (path.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
           || path.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
        {
            WebClient client = new WebClient();
            var contents = client.DownloadString(path);
            return contents;
        }
        else
        {
            return File.ReadAllText(path);
        }
    }

    public Content GetContent(string path)
    {
        var text = GetTextFromPath(path);
        return contentSerializer.Deserialize(text);
    }

    public IEnumerable<BatchStatus> ListBatches()
        => this.datastore.Query(db => db.ListBatches()
            .Select(BatchStatusRecord.AsBatchStatus)
            .ToList()
        );

    public BatchStatus? GetBatch(int id)
        => this.datastore.Query(db => {
            var record = db.GetBatch(id);
            if (record == null)
                return null;

            return BatchStatusRecord.AsBatchStatus(record);
        });

    public IEnumerable<Variable> ListGlobalVariables()
        => this.datastore.Query(db => db.ListGlobalVariables()
            .Select(VariableRecord.AsVariable)
            .ToList()
        );

    public IEnumerable<Variable> ListBatchVariables(int batchId)
        => this.datastore.Query(db => db.ListBatchVariables(batchId)
            .Select(VariableRecord.AsVariable)
            .ToList()
        );

    public IEnumerable<BatchContent> ListBatchContent(int batchId)
        => this.datastore.Query(db => db.ListBatchContent(batchId)
            .Select(record =>
            {
                Content? content = record.Content == null ? null : contentSerializer.Deserialize(record.Content);
                return new BatchContent(
                    record.Id,
                    record.ContentPath,
                    content,
                    (BatchState)record.StageId
                );
            })
            .ToList()
        );

    public void UpdateGlobalVariables(Variable[] variables)
        => this.datastore.ExecuteInTransaction((db, _) =>
        {
            db.DeleteAllGlobalVariables();
            foreach (var variable in variables)
                db.InsertVariable(variable);
        });

    public void Requeue(int id)
        => this.datastore.ExecuteInTransaction((db, _) =>
            db.ReqeueBatch(id));

    public void QueueBatch(string name, Variable[] variables, IEnumerable<(Content? Content, string? Path)> contents)
        => this.datastore.ExecuteInTransaction((db, _) =>
        {
            var batchId = db.CreateNewBatch(name);
            foreach (var variable in variables)
            {
                db.InsertBatchVariable(batchId, variable);
            }

            int priority = 0;
            foreach (var (content, url) in contents)
            {
                if (content != null)
                    db.InsertBatchContent(batchId, priority++, contentSerializer.Serialize(content), null);
                else if (url != null)
                    db.InsertBatchContent(batchId, priority++, null, url.ToString());
            }
        });

    public void QueueUrl(string name, Variable[] variables, string url)
    {
        var text = GetTextFromPath(url);
        var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        var paths = lines.Select(line => ((Content?)null, (string?)line));
        QueueBatch(name, variables, paths);
    }

    public void DeleteBatch(int batchId)
        => datastore.ExecuteInTransaction((db, _) =>
        {
            db.DeleteBatchLog(batchId);
            db.DeleteBatchContent(batchId);
            db.DeleteBatchVariables(batchId);
            db.DeleteBatch(batchId);
        });

    public void StartBatchContent(int id)
        => datastore.Execute(db => db.StartBatchContent(id));

    public void CompleteBatchContent(int id)
        => datastore.Execute(db => db.CompleteBatchContent(id));

    public void StartBatch(int id)
        => datastore.Execute(db => db.StartBatch(id));

    public void CompleteBatch(int id)
        => datastore.Execute(db => db.CompleteBatch(id));

    public void Log(int batchId, int? contentId, string message)
        => datastore.Execute(db => db.InsertNewLogEntry(batchId, contentId, message));

    public string GetLog(int id)
        => datastore.Query(db => {
            var builder = new StringBuilder();
            var record = db.GetLog(id);
            foreach (var item in record)
                builder.Append(item).Append(Environment.NewLine);
            return builder.ToString();
        });

    public void WipeDatabase()
    {
        var topIds = datastore.Query(db => db.GetTopBatchId().ToList());
        while (topIds.Any())
        {
            var id = topIds.First();

            // Delete log entries in batches of 10
            var logEntries = datastore.Query(db => db.GetTopLogEntryIds(id).ToList());
            while (logEntries.Any())
            {
                datastore.Execute(db =>
                {
                    foreach (var id in logEntries)
                        db.DeleteLogEntry(id);
                });

                logEntries = datastore.Query(db => db.GetTopLogEntryIds(id).ToList());
            }

            // Delete variables
            datastore.Execute(db => db.DeleteBatchVariables(id));

            // Delete content in batches of 10
            var contentIds = datastore.Query(db => db.GetTopContentIds(id).ToList());
            while (contentIds.Any())
            {
                datastore.Execute(db =>
                {
                    foreach (var id in contentIds)
                        db.DeleteContent(id);
                });

                contentIds = datastore.Query(db => db.GetTopContentIds(id).ToList());
            }

            // Finally delete the content
            datastore.Execute(db => db.DeleteBatch(id));

            // Repeat
            topIds = datastore.Query(db => db.GetTopBatchId().ToList());
        }
    }
}
