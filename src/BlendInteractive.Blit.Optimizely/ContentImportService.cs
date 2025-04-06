using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Blobs;
using EPiServer.Web;
using EPiServer;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml;

namespace BlendInteractive.Blit.Optimizely;

public class ContentImportService
{
    private readonly IContentRepository contentRepository;
    private readonly IBlobFactory blobFactory;
    private readonly IContentQueryResolver queryResolver;
    private readonly IBatchService batchService;
    private readonly CategoryRepository categoryRepository;

    public ContentImportService(IContentRepository contentRepository, IBlobFactory blobFactory, IContentQueryResolver queryResolver, IBatchService batchService, CategoryRepository categoryRepository)
    {
        this.contentRepository = contentRepository;
        this.blobFactory = blobFactory;
        this.queryResolver = queryResolver;
        this.batchService = batchService;
        this.categoryRepository = categoryRepository;
    }


    public bool ProcessBatch(Action<ImportStatus>? onStatusUpdate)
    {
        var batches = batchService.ListBatches();

        var next = batches.Where(x => x.State == BatchState.Queued)
            .OrderBy(x => x.Date)
            .FirstOrDefault();

        if (next == null)
            return false;

        try
        {
            ProcessBatch(next, onStatusUpdate);
        }
        catch (Exception ex)
        {
            batchService.Log(next.Id, null, $"Uncaught batch error: {ex.Message} | {ex.StackTrace}");
        }

        return true;
    }

    private void ProcessBatch(BatchStatus next, Action<ImportStatus>? onStatusUpdate)
    {
        batchService.StartBatch(next.Id);

        var globalVariables = batchService.ListGlobalVariables();
        var batchVariables = batchService.ListBatchVariables(next.Id);
        var batchContent = batchService.ListBatchContent(next.Id);

        var context = ImportContext.Create(next.Id, batchVariables, globalVariables);

        // Content that may need to be updated on the second pass
        List<(int ContentId, ContentReference OptiContent, Content Content)> secondPassContent = new List<(int, ContentReference, Content)>();

        var queuedFiles = batchContent.Where(x => x.State == BatchState.Queued).ToList();
        var status = new ImportStatus(0, 0, queuedFiles.Count);

        // Stage one
        foreach (var file in queuedFiles)
        {
            Content content;
            try
            {
                if (file.Path is not null)
                {
                    content = batchService.GetContent(file.Path);
                }
                else if (file.Content is not null)
                {
                    content = file.Content;
                }
                else
                {
                    throw new InvalidOperationException($"Batch content has neither a path nor content.");
                }
            }
            catch (Exception ex)
            {
                batchService.Log(context.BatchId, file.Id, $"Error processing first-pass content. File ID: {file.Id}, Possible path: {file.Path} - {ex.Message} - {ex.StackTrace}");
                return;
            }

            try
            {
                batchService.StartBatchContent(file.Id);
                status = status with { FirstPassComplete = status.FirstPassComplete + 1 };
                onStatusUpdate?.Invoke(status);

                var updatedContent = FirstPass(context, content);
                if (updatedContent != null)
                {
                    secondPassContent.Add((file.Id, updatedContent, content));
                }
                else
                {
                    batchService.CompleteBatchContent(file.Id);
                    status = status with { SecondPassComplete = status.SecondPassComplete + 1 };
                    onStatusUpdate?.Invoke(status);
                }
            }
            catch (Exception ex)
            {
                batchService.Log(context.BatchId, file.Id, $"Error processing first-pass content. Content ID: {content.Id} - {ex.Message} - {ex.StackTrace}");
            }
        }

        // Second pass (to update referenced content)
        foreach (var update in secondPassContent)
        {
            try
            {
                var optiContent = contentRepository.Get<IContent>(update.OptiContent);
                var updatedContext = context.WithStage(ImportStage.StageTwo)
                    .WithPage(optiContent);

                SecondPass(updatedContext, update.Content);
                batchService.CompleteBatchContent(update.ContentId);
                status = status with { SecondPassComplete = status.SecondPassComplete + 1 };
                onStatusUpdate?.Invoke(status);
            }
            catch (Exception ex)
            {
                batchService.Log(context.BatchId, update.ContentId, $"Error processing second-pass content. Content ID: {update.Content.Id} - {ex.Message} - {ex.StackTrace}");
            }
        }

        batchService.CompleteBatch(next.Id);
    }


    private ContentReference? FirstPass(ImportContext context, Content content)
    {
        var existingContent = queryResolver.FindContent(context, content.Query);

        if (existingContent == null && content.Actions.HasFlag(ContentActions.Create))
        {
            return CreateNewContent(context, content);
        }
        else if (existingContent != null)
        {
            if (context.CurrentContent == null)
                context = context.WithPage(existingContent);

            if (content.Actions.HasFlag(ContentActions.Delete))
            {
                contentRepository.Delete(existingContent.ContentLink, true, EPiServer.Security.AccessLevel.NoAccess);
                batchService.Log(context.BatchId, null, $"Deleted content. CMS ID: {existingContent.ContentLink.ID}, Content Id: {content.Id}");
                return null;
            }

            if (content.Actions.HasFlag(ContentActions.Update))
            {
                UpdateExistingContent(context, existingContent, content);
            }
            if (content.Actions.HasFlag(ContentActions.Move))
            {
                throw new NotImplementedException();
            }

            return existingContent.ContentLink;
        }
        else
        {
            batchService.Log(context.BatchId, null, $"Could not find non-create content. Content Id: {content.Id}");
            return null;
        }
    }

    private void SecondPass(ImportContext context, Content content)
    {
        if (context.CurrentContent == null)
            throw new InvalidOperationException($"Cannot do second pass on non-existant content. Content Id: {content.Id}");

        UpdateExistingContent(context, context.CurrentContent, content);
    }


    private ContentReference CreateNewContent(ImportContext context, Content content)
    {
        var parent = queryResolver.FindContent(context, content.Parent!);
        if (parent == null)
            throw new InvalidOperationException($"Could create new content. Parent could not be located. Content Id: {content.Id}");

        var contentType = Type.GetType(content.Type);
        if (contentType == null)
            throw new InvalidOperationException($"Could create new content. Could not find type: {content.Type}. Content Id: {content.Id}");

        // Same as: var newContent = this.contentRepository.GetDefault<T>(parent.ContentLink);

        IContent newContent = contentRepository.CreateContent(contentType, parent.ContentLink);
        if (content.StageOne != null)
            ApplyProperties(content.Id, context, content.StageOne, newContent);

        var newContentId = contentRepository.Save(newContent, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);

        batchService.Log(context.BatchId, null, $"Created new content: CMS Id: {newContentId.ID}, Content Id: {content.Id}");

        return newContentId;
    }

    private void UpdateExistingContent(ImportContext context, IContent optiContent, Content content)
    {
        var updatedCopy = ((IReadOnly)optiContent).CreateWritableClone();

        bool anyChanges = false;
        var properties = context.ImportStage == ImportStage.StageOne ? content.StageOne : content.StageTwo;
        if (properties != null)
            anyChanges = ApplyProperties(content.Id, context, properties, updatedCopy);

        if (anyChanges)
        {
            contentRepository.Save((IContent)updatedCopy, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);
            batchService.Log(context.BatchId, null, $"Updated content. Stage: {context.ImportStage}, CMS ID: {optiContent.ContentLink.ID}, Content Id: {content.Id}");
        }
        else
        {
            batchService.Log(context.BatchId, null, $"No changes detected. Stage: {context.ImportStage}, CMS ID: {optiContent.ContentLink.ID}, Content Id: {content.Id}");
        }
    }

    private bool ApplyProperties(string contentId, ImportContext context, IEnumerable<IProperty> properties, object optiContent)
    {
        bool changed = false;

        foreach (var prop in properties)
        {
            object? existingValue = GetPropertyValue(optiContent, prop.Name);
            switch (prop)
            {
                case TextProperty text:
                    var newTextValue = ResolveFragments(contentId, context, text.Fragments);
                    var propType = GetPropertyType(optiContent, text.Name);
                    if (propType == null)
                        throw new InvalidOperationException($"Could not locate property {text.Name} on type {optiContent.GetType().GetShortTypeName()}. Content Id: {contentId}");

                    var (newValue, valueChanged) = ApplyProperty(propType, existingValue, newTextValue);
                    if (valueChanged)
                    {
                        SetPropertyValue(optiContent, prop.Name, newValue);
                        changed = true;
                    }

                    break;
                case BlobProperty blob:
                    if (optiContent is not IBinaryStorable binaryStorable)
                        throw new InvalidOperationException($"Cannot add Blob to non-IBinaryStorable properties. Content type: {optiContent.GetType().GetShortTypeName()}. Property Name: {blob.Name}. Content Id: {contentId}");

                    var existingBlob = existingValue as Blob;
                    var existingBytes = existingBlob != null ? existingBlob.ReadAllBytes() : null;
                    var newBytes = blob.Data;

                    if (existingBytes == null || existingBytes.Length != newBytes.Length || !existingBytes.SequenceEqual(newBytes))
                    {
                        var newBlob = blobFactory.CreateBlob(binaryStorable.BinaryDataContainer, blob.FileExtension);
                        newBlob.WriteAllBytes(newBytes);
                        SetPropertyValue(optiContent, blob.Name, newBlob);
                        changed = true;
                    }

                    break;
                case NestedProperty nested:
                    var contentData = existingValue as IContentData;
                    if (contentData == null)
                    {
                        throw new NotImplementedException($"Unable to create new nested content. Content type: {optiContent.GetType().GetShortTypeName()}. Property Name: {nested.Name}. Content Id: {contentId}");
                    }
                    changed = ApplyProperties(contentId, context, nested.Properties, contentData) || changed;
                    break;
                case ListProperty list:
                    var newTextList = list.Items.Select(x => ResolveFragments(contentId, context, x.Fragments)).ToArray();

                    var listPropType = GetPropertyType(optiContent, list.Name);
                    if (listPropType == null)
                        throw new InvalidOperationException($"Could not locate property {list.Name} on type {optiContent.GetType().GetShortTypeName()}. Content Id: {contentId}");

                    var (listNewValue, listValueChanged) = ApplyListProperty(listPropType, existingValue, newTextList);
                    if (listValueChanged)
                    {
                        SetPropertyValue(optiContent, prop.Name, listNewValue);
                        changed = true;
                    }

                    break;
                default:
                    throw new NotImplementedException($"Could not apply property of type {prop.GetType().FullName}.  Content type: {optiContent.GetType().GetShortTypeName()}. Property Name: {prop.Name}. Content Id: {contentId}");
            }
        }

        return changed;
    }

    protected virtual object? GetPropertyValue(object content, string propertyName)
    {
        bool ReadSpecialProperty(string name, Func<PageData, object?> getValue, out object? value)
        {
            if (propertyName != name)
            {
                value = default;
                return false;
            }

            if (content is not PageData pageData)
                throw new InvalidOperationException($"Cannot read `{propertyName}` property on non-pagedata object: {content.GetType().FullName}.");

            value = getValue(pageData);
            return true;
        }

        if (ReadSpecialProperty("PageTargetFrame", pageData => pageData["PageTargetFrame"], out var targetFrame))
            return targetFrame;

        if (ReadSpecialProperty("PageShortcutLink", pageData => pageData["PageShortcutLink"], out var pageShortcutLink))
            return pageShortcutLink;

        return content.GetPropertyValue(propertyName);
    }

    protected virtual void SetPropertyValue(object content, string propertyName, object? value)
    {
        bool WriteSpecialProperty(string name, Action<PageData> setValue)
        {
            if (propertyName != name)
                return false;

            if (content is not PageData pageData)
                throw new InvalidOperationException($"Cannot read `{propertyName}` property on non-pagedata object: {content.GetType().FullName}.");

            setValue(pageData);
            return true;
        }

        if (WriteSpecialProperty("PageTargetFrame", pageData => pageData["PageTargetFrame"] = value))
            return;

        if (WriteSpecialProperty("PageShortcutLink", pageData => pageData["PageShortcutLink"] = value))
            return;

        content.SetPropertyValue(propertyName, value);
    }

    protected virtual Type? GetPropertyType(object content, string propertyName)
    {
        return propertyName switch
        {
            "PageShortcutLink" => typeof(ContentReference),
            "PageTargetFrame" => typeof(string),
            _ => content.GetPropertyType(propertyName)
        };
    }

    protected virtual (object? NewValue, bool Changed) ApplyListProperty(Type propertyType, object? existingValue, string[] newTextValue)
    {
        if (typeof(ContentArea).IsAssignableFrom(propertyType))
        {
            ContentArea? contentArea = existingValue as ContentArea;
            if (contentArea == null)
                contentArea = new ContentArea();

            var itemIds = contentArea.Items.Select(x => x.ContentLink.ID);
            var newIds = newTextValue.Select(x => int.Parse(x));

            if (itemIds.SequenceEqual(newIds))
                return (null, false);

            contentArea.Items.Clear();
            foreach (var newId in newIds)
            {
                contentArea.Items.Add(new ContentAreaItem
                {
                    ContentLink = new ContentReference(newId)
                });
            }

            return (contentArea, true);
        }

        if (typeof(CategoryList).IsAssignableFrom(propertyType))
        {
            var newIds = newTextValue.Select(x => int.Parse(x));
            var categoryList = existingValue as CategoryList;
            if (categoryList == null)
                categoryList = new CategoryList();

            if (newIds.SequenceEqual(categoryList))
                return (null, false);

            categoryList.Clear();

            foreach (var newId in newIds)
                categoryList.Add(newId);

            return (categoryList, true);
        }

        throw new NotImplementedException($"Unable to set property type {propertyType.FullName}");
    }

    protected virtual (object? NewValue, bool Changed) ApplyProperty(Type propertyType, object? existingValue, string newTextValue)
    {
        if (typeof(XhtmlString).IsAssignableFrom(propertyType))
        {
            var currentHtml = (existingValue as XhtmlString)?.ToInternalString();
            if (currentHtml == newTextValue)
                return (null, false);

            return (new XhtmlString(newTextValue), true);
        }


        if (typeof(DateTime?).IsAssignableFrom(propertyType))
        {
            var currentDate = existingValue as DateTime?;
            var newDate = string.IsNullOrEmpty(newTextValue) ? (DateTime?)null : DateTime.Parse(newTextValue);
            if (newDate == currentDate)
                return (null, false);

            return (newDate, true);
        }

        if (typeof(int?).IsAssignableFrom(propertyType))
        {
            var currentInteger = existingValue as int?;
            var newInteger = string.IsNullOrEmpty(newTextValue) ? (int?)null : int.Parse(newTextValue);
            if (newInteger == currentInteger)
                return (null, false);

            return (newInteger, true);
        }

        if (typeof(Url).IsAssignableFrom(propertyType))
        {
            var currentUrl = existingValue as Url;
            var existingUrlString = currentUrl?.ToString();

            if (existingUrlString == newTextValue)
                return (null, false);

            var newUrl = string.IsNullOrEmpty(newTextValue) ? null : new Url(newTextValue);
            return (newUrl, true);
        }

        if (typeof(ContentReference).IsAssignableFrom(propertyType))
        {
            var currentReference = existingValue as ContentReference;

            ContentReference newContentLink;
            if (!string.IsNullOrEmpty(newTextValue))
            {
                newContentLink = new ContentReference(newTextValue);
            }
            else
            {
                newContentLink = ContentReference.EmptyReference;
            }

            if ((currentReference ?? ContentReference.EmptyReference).CompareToIgnoreWorkID(newContentLink))
                return (null, false);

            return (newContentLink, true);
        }

        if (propertyType.IsEnum)
        {
            var parsedExisting = existingValue is null ? Enum.GetValues(propertyType).GetValue(0) : Enum.Parse(propertyType, existingValue.ToString()!);
            var parsedNew = newTextValue == null ? Enum.GetValues(propertyType).GetValue(0) : Enum.Parse(propertyType, newTextValue);

            if (parsedExisting is null || !parsedExisting.Equals(parsedNew))
            {
                return (parsedNew, true);
            }
            return (default, false);
        }

        if (typeof(PageShortcutType).IsAssignableFrom(propertyType))
        {
            PageShortcutType currentShortcutType = existingValue is null ? PageShortcutType.Normal : (PageShortcutType)existingValue;
            PageShortcutType newShortcutType = string.IsNullOrEmpty(newTextValue) ? PageShortcutType.Normal : Enum.Parse<PageShortcutType>(newTextValue);

            if (currentShortcutType == newShortcutType)
                return (null, false);

            return (newShortcutType, true);
        }

        if (typeof(string).IsAssignableFrom(propertyType))
        {
            var currentText = existingValue as string;
            if (currentText == newTextValue)
                return (null, false);

            return (newTextValue, true);
        }

        throw new NotImplementedException($"Unable to set property type {propertyType.FullName}");
    }

    private string ResolveFragments(string contentId, ImportContext context, IEnumerable<IFragment> fragments)
    {
        var buffer = new StringBuilder();
        foreach (var fragment in fragments)
        {
            switch (fragment)
            {
                case TextFragment text:
                    buffer.Append(text.Text);
                    break;
                case VariableReference variable:
                    var value = context.GetVariableValue(variable.Name);
                    if (value == null)
                        throw new InvalidOperationException($"Variable does not exist {variable.Name}. Content Id: {contentId}");
                    buffer.Append(value);
                    break;
                case InlineContentFragment inlineContent:
                    var childContext = context.Child()
                        .WithStage(ImportStage.StageOne);

                    var file = FirstPass(childContext, inlineContent.Content);
                    if (file != null)
                    {
                        var childContent = contentRepository.Get<IContent>(file);
                        var secondContext = childContext.WithStage(ImportStage.StageTwo)
                            .WithPage(childContent);
                        SecondPass(secondContext, inlineContent.Content);
                    }
                    buffer.Append(GetContentLink(inlineContent.EmbedType, file));
                    break;
                case ContentLookupFragment lookup:
                    var reference = queryResolver.FindReference(context, lookup.Query);
                    buffer.Append(GetContentLink(lookup.EmbedType, reference, lookup.FallbackUrl));
                    break;
                case CategoryPathReference categoryPath:
                    Category category = categoryRepository.GetRoot();
                    foreach (var pathName in categoryPath.CategoryPath)
                    {
                        var resolvedCategoryName = ResolveFragments(contentId, context, pathName.Fragments);
                        Category? childCategory = category.FindChild(resolvedCategoryName);
                        if (childCategory == null)
                        {
                            childCategory = new Category(category, resolvedCategoryName);
                            childCategory.Description = resolvedCategoryName;
                            categoryRepository.Save(childCategory);
                        }
                        else
                        {
                            category = childCategory;
                        }
                    }

                    buffer.Append(category.ID.ToString());
                    break;
                default:
                    throw new NotImplementedException($"No resolver for {fragment.GetType().FullName}. Content Id: {contentId}");
            }
        }
        return buffer.ToString();
    }

    private string GetContentLink(ContentEmbedType embedType, ContentReference? contentReference, string fallback = "")
    {
        if (contentReference == null || ContentReference.IsNullOrEmpty(contentReference))
            return fallback;

        switch (embedType)
        {
            case ContentEmbedType.PermanentUrl:
                var guid = PermanentLinkUtility.FindGuid(contentReference);
                return PermanentLinkUtility.GetPermanentLinkUrl(guid, ".aspx").ToString();
            case ContentEmbedType.ID:
                return contentReference.ID.ToString();
            case ContentEmbedType.EmbeddedBlock:
                var content = contentRepository.Get<IContent>(contentReference);
                if (content == null)
                    throw new InvalidOperationException($"Could not get content from reference: {contentReference.ID} for block embed");

                // <div class="epi-contentfragment" data-classid="36f4349b-8093-492b-b616-05d8964e4c89" data-contentguid="7073f2da-106a-4148-9984-75b1eaa90cb7" data-contentlink="230" data-contentname="Raw code block example" contenteditable="false">Raw code block example</div>
                var blockGuid = PermanentLinkUtility.FindGuid(contentReference);
                var xhtml = new XElement("div",
                    new XAttribute("class", "epi-contentfragment"),
                    new XAttribute("data-classid", "36f4349b-8093-492b-b616-05d8964e4c89"),
                    new XAttribute("data-contentguid", blockGuid.ToString()),
                    new XAttribute("data-contentlink", contentReference.ID.ToString()),
                    new XAttribute("data-contentname", content.Name),
                    new XAttribute("contenteditable", "false"),
                    new XText(content.Name)
                );

                var htmlWriter = new StringWriter();
                var xmlWriter = XmlWriter.Create(htmlWriter);
                xhtml.WriteTo(xmlWriter);
                xmlWriter.Flush();
                return htmlWriter.ToString();
            default:
                throw new NotImplementedException($"No way to resolve link with embed type: {embedType}");
        }
    }
}
