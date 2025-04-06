using EPiServer.Core;

namespace BlendInteractive.Blit.Optimizely;

public class ImportContext
{
    public static ImportContext Create(int batchId, IEnumerable<Variable> batchVariables, IEnumerable<Variable> globalVariables)
    {
        var variableValues = new Dictionary<string, string>();
        foreach (var variable in globalVariables)
            variableValues[variable.Name] = variable.Value;
        foreach (var variable in batchVariables)
            variableValues[variable.Name] = variable.Value;

        return new ImportContext(null, batchId, variableValues, null, ImportStage.StageOne);
    }

    private ImportContext(ImportContext? parentContext, int batchId, Dictionary<string, string> variables, IContent? currentPage, ImportStage importStage)
    {
        ParentContext = parentContext;
        BatchId = batchId;
        this.variables = variables;
        this.currentPage = currentPage;
        ImportStage = importStage;
    }

    private readonly Dictionary<string, string> variables;

    public ImportContext? ParentContext { get; }
    public int BatchId { get; }

    private readonly IContent? currentPage;
    public ImportStage ImportStage { get; }

    public string? GetVariableValue(string name) => variables.TryGetValue(name, out var value) ? value : null;

    public IContent? CurrentContent
    {
        get
        {
            if (currentPage is not null)
                return currentPage;
            if (ParentContext is not null)
                return ParentContext.CurrentContent;
            return null;
        }
    }

    public PageData? CurrentPage
    {
        get
        {
            if (currentPage is not null && currentPage is PageData pageData)
                return pageData;
            if (ParentContext is not null)
                return ParentContext.CurrentPage;
            return null;
        }
    }


    public ImportContext WithPage(IContent page) => new ImportContext(ParentContext, BatchId, variables, page, ImportStage);

    public ImportContext Child() => new ImportContext(this, BatchId, variables, currentPage, ImportStage);

    public ImportContext WithStage(ImportStage stage) => new ImportContext(ParentContext, BatchId, variables, currentPage, stage);
}