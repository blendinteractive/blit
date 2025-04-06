namespace BlendInteractive.Blit.Optimizely;

public interface IBatchService
{
    Content GetContent(string id);

    IEnumerable<BatchStatus> ListBatches();

    BatchStatus? GetBatch(int id);

    IEnumerable<Variable> ListGlobalVariables();

    IEnumerable<Variable> ListBatchVariables(int batchId);

    string GetLog(int id);

    IEnumerable<BatchContent> ListBatchContent(int batchId);
    void Requeue(int id);

    void StartBatchContent(int id);

    void CompleteBatchContent(int id);

    void UpdateGlobalVariables(Variable[] variables);

    void QueueBatch(string name, Variable[] variables, IEnumerable<(Content? Content, string? Path)> contents);

    void QueueUrl(string name, Variable[] variables, string url);

    void DeleteBatch(int batchId);

    void StartBatch(int id);

    void CompleteBatch(int id);

    void Log(int batchId, int? contentId, string message);

    void WipeDatabase();
}
