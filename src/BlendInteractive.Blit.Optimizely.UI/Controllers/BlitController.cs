using BlendInteractive.Blit.Optimizely.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlendInteractive.Blit.Optimizely.UI.Controllers;

[Authorize(Roles = "Administrators, WebAdmins, BlendImportAdmin")]
public partial class BlitController(IBatchService batchService) : Controller
{
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true,
    };

    public ActionResult Index()
    {
        var batches = batchService.ListBatches();
        var viewModel = new IndexViewModule(batches);
        return View(viewModel);
    }

    [HttpGet]
    public ActionResult QueueUrl()
    {
        var viewModel = new QueueUrlViewModel()
        {
            VariblesJson = "{}"
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult QueueUrl(QueueUrlViewModel model)
    {
        batchService.QueueUrl(model.FriendlyName!, DeserializeVariables(model.VariblesJson), model.Url!);
        return Redirect(model.Link(null));
    }

    [HttpGet]
    public ActionResult GlobalVars()
    {
        var globalVars = batchService.ListGlobalVariables();



        var model = new GlobalVarsViewModel
        {
            VariblesJson = SerializeVariables(globalVars),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult GlobalVars(GlobalVarsViewModel model)
    {
        if (model.VariblesJson is null)
        {
            return View(model);
        }

        var variables = DeserializeVariables(model.VariblesJson);
        batchService.UpdateGlobalVariables(variables.ToArray());

        return Redirect(model.Link(null));
    }

    [HttpGet]
    public ActionResult ViewBatch(int batchId)
    {

        var batchDetails = batchService.GetBatch(batchId);
        var log = batchService.GetLog(batchId);

        var viewModel = new ViewBatchViewModel
        {
            BatchId = batchId,
            Details = batchDetails,
            Log = log
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Requeue(ViewBatchViewModel model)
    {
        batchService.Requeue(model.BatchId);
        return Redirect(model.Link(null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(ViewBatchViewModel model)
    {
        batchService.DeleteBatch(model.BatchId);
        return Redirect(model.Link(null));
    }

    private Variable[] DeserializeVariables(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return Array.Empty<Variable>();

        var parsed = JsonSerializer.Deserialize<JsonObject>(raw)!;
        var variables = new List<Variable>();

        foreach (var kvp in parsed)
        {
            var key = kvp.Key;
            var value = kvp.Value!.GetValue<string>()!;

            variables.Add(new Variable(key, value));
        }

        return variables.ToArray();
    }

    private string SerializeVariables(IEnumerable<Variable> variables)
    {
        var jsonObject = new JsonObject();
        foreach (var item in variables)
        {
            jsonObject[item.Name] = item.Value;
        }

        return JsonSerializer.Serialize(jsonObject, serializerOptions);
    }
}
