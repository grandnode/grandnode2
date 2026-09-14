using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.TagHelpers.Admin.Grid;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Globalization;
using System.Text.Json;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     Declarative admin grid rendered by admin.grid.js (Tabulator) against the unchanged
///     DataSourceRequest/DataSourceResult contract. Emits
///     &lt;div id data-role="grid" data-grand-grid='{json}'&gt; plus inert &lt;template&gt;
///     elements for cell templates. Child elements: grid-column, grid-commands,
///     grid-toolbar-create, grid-detail, grid-text.
/// </summary>
[HtmlTargetElement("admin-grid", Attributes = "id")]
[RestrictChildren("grid-column", "grid-commands", "grid-toolbar-create", "grid-toolbar-save", "grid-toolbar-cancel",
    "grid-detail", "grid-text")]
public class AdminGridTagHelper : TagHelper
{
    internal static readonly object ContextKey = typeof(GridBuilder);

    private readonly AdminAreaSettings _adminAreaSettings;
    private readonly IContextAccessor _contextAccessor;
    private readonly LanguageSettings _languageSettings;
    private readonly ITranslationService _translationService;
    private readonly IUrlHelperFactory _urlHelperFactory;

    public AdminGridTagHelper(
        AdminAreaSettings adminAreaSettings,
        LanguageSettings languageSettings,
        IContextAccessor contextAccessor,
        ITranslationService translationService,
        IUrlHelperFactory urlHelperFactory)
    {
        _adminAreaSettings = adminAreaSettings;
        _languageSettings = languageSettings;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _urlHelperFactory = urlHelperFactory;
    }

    [HtmlAttributeName("id")] public string Id { get; set; }

    /// <summary>Id field of the rows (schema.model.id). Default "Id".</summary>
    public string Key { get; set; } = "Id";

    public string ReadUrl { get; set; }
    public string CreateUrl { get; set; }
    public string UpdateUrl { get; set; }
    public string DestroyUrl { get; set; }

    /// <summary>Controller for the *-action attributes; the area comes from the current route.</summary>
    public string Controller { get; set; }

    public string ReadAction { get; set; }
    public string CreateAction { get; set; }
    public string UpdateAction { get; set; }
    public string DestroyAction { get; set; }

    /// <summary>Page size; defaults to AdminAreaSettings.DefaultGridPageSize for the Full pager.</summary>
    public int? PageSize { get; set; }

    /// <summary>Comma separated page sizes; defaults to AdminAreaSettings.GridPageSizes.</summary>
    public string PageSizes { get; set; }

    public GridPager Pager { get; set; } = GridPager.Full;

    /// <summary>
    ///     false: the read URL returns every row and the grid cuts the pages itself, sending no
    ///     paging fields (a Kendo data source without serverPaging).
    /// </summary>
    public bool ServerPaging { get; set; } = true;

    /// <summary>false for grids loaded by tabstrip_on_tab_show (Kendo autoBind: false).</summary>
    public bool AutoBind { get; set; } = true;

    public GridEditMode EditMode { get; set; } = GridEditMode.None;

    /// <summary>Reloads the page after update (Kendo requestEnd: this.read()). Create always reloads.</summary>
    public bool ReloadAfterSave { get; set; } = true;

    /// <summary>Asks before destroy (Kendo editable.confirmation).</summary>
    public bool ConfirmDestroy { get; set; }

    public GridSelectable Selectable { get; set; } = GridSelectable.None;

    /// <summary>
    ///     Name of the checkbox inputs of selectable="Checkbox", so the ticked rows of the page are
    ///     posted with the surrounding form (e.g. SelectedProductIds).
    /// </summary>
    public string SelectionName { get; set; }

    /// <summary>
    ///     edit-mode="Batch": posts all changed rows in one request as prefix[i].Field (the
    ///     Kendo batch parameterMap pattern). Without it each row is posted on its own.
    /// </summary>
    public string BatchPrefix { get; set; }

    /// <summary>Rows rendered with the page for a grid without read-url (Kendo dataSource.data).</summary>
    public object Data { get; set; }

    /// <summary>Name of a global function returning extra read data (Kendo transport.read.data).</summary>
    public string AdditionalData { get; set; }

    /// <summary>Selector of a container whose fields are posted with every read.</summary>
    public string SearchForm { get; set; }

    public string OnDataBound { get; set; }
    public string OnEdit { get; set; }
    public string OnSave { get; set; }
    public string OnChange { get; set; }
    public string OnRequestStart { get; set; }
    public string OnRequestEnd { get; set; }
    public string OnError { get; set; }
    public string OnDetailInit { get; set; }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var builder = new GridBuilder(new GridDefinition(), Id);
        context.Items[ContextKey] = builder;

        await output.GetChildContentAsync();

        var grid = builder.Grid;
        grid.Key = Key;
        grid.Transport = new GridTransport {
            Read = ResolveUrl(ReadUrl, Controller, ReadAction),
            Create = ResolveUrl(CreateUrl, Controller, CreateAction),
            Update = ResolveUrl(UpdateUrl, Controller, UpdateAction),
            Destroy = ResolveUrl(DestroyUrl, Controller, DestroyAction)
        };
        grid.Pager = Pager;
        grid.PageSize = PageSize ?? (Pager == GridPager.Full ? _adminAreaSettings.DefaultGridPageSize : null);
        if (Pager == GridPager.Full)
            grid.PageSizes = ParsePageSizes(PageSizes ?? _adminAreaSettings.GridPageSizes);
        grid.ServerPaging = ServerPaging ? null : false;
        grid.AutoBind = AutoBind ? null : false;
        grid.EditMode = EditMode;
        grid.ReloadAfterSave = ReloadAfterSave ? null : false;
        grid.ConfirmDestroy = ConfirmDestroy ? true : null;
        grid.Selectable = Selectable;
        grid.SelectionName = SelectionName;
        grid.BatchPrefix = BatchPrefix;
        //own serializer: the rows keep their property names (the grid options are camelCase)
        grid.Data = Data is null ? null : JsonSerializer.SerializeToElement(Data);
        grid.AdditionalData = AdditionalData;
        grid.SearchForm = SearchForm;
        grid.Events = Events();
        grid.Texts = Texts(builder.Texts);
        grid.Culture = GridCulture.From(CultureInfo.CurrentCulture);
        grid.Rtl = IsRtl() ? true : null;

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", Id);
        output.Attributes.SetAttribute("data-role", "grid");
        output.Attributes.SetAttribute("data-grand-grid", grid.ToJson());
        output.Content.Clear();
        foreach (var (templateId, html) in builder.Templates)
        {
            var template = new TagBuilder("template");
            template.Attributes["id"] = templateId;
            template.InnerHtml.AppendHtml(html);
            output.PostElement.AppendHtml(template);
        }
    }

    internal static IList<int> ParsePageSizes(string pageSizes)
    {
        if (string.IsNullOrWhiteSpace(pageSizes))
            return null;
        var sizes = pageSizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) ? size : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        return sizes.Count > 0 ? sizes : null;
    }

    internal string ResolveUrl(string url, string controller, string action)
    {
        if (!string.IsNullOrEmpty(url) || string.IsNullOrEmpty(action))
            return string.IsNullOrEmpty(url) ? null : url;
        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
        var area = ViewContext.RouteData.Values["area"]?.ToString();
        return urlHelper.Action(action, controller, new { area });
    }

    private bool IsRtl()
    {
        var language = _contextAccessor.WorkContext?.WorkingLanguage;
        //same condition as HeadAdmin/HeadStore/HeadVendor
        return language is { Rtl: true } && !_languageSettings.IgnoreRtlPropertyForAdminArea;
    }

    private IDictionary<string, string> Events()
    {
        var events = new Dictionary<string, string>();
        void Add(string name, string handler)
        {
            if (!string.IsNullOrWhiteSpace(handler)) events[name] = handler;
        }

        Add("dataBound", OnDataBound);
        Add("edit", OnEdit);
        Add("save", OnSave);
        Add("change", OnChange);
        Add("requestStart", OnRequestStart);
        Add("requestEnd", OnRequestEnd);
        Add("error", OnError);
        Add("detailInit", OnDetailInit);
        return events.Count > 0 ? events : null;
    }

    private IDictionary<string, string> Texts(IDictionary<string, string> custom)
    {
        var keys = new Dictionary<string, string> {
            ["edit"] = "Admin.Common.Edit",
            ["update"] = "Admin.Common.Update",
            ["cancel"] = "Admin.Common.Cancel",
            ["delete"] = "Admin.Common.Delete",
            ["areYouSure"] = "Admin.Common.AreYouSure",
            ["deleteConfirmation"] = "Admin.Common.DeleteConfirmation",
            ["refresh"] = "Admin.Common.Grid.Refresh",
            ["itemsPerPage"] = "Admin.Common.Grid.ItemsPerPage",
            ["pageInfo"] = "Admin.Common.Grid.PageInfo",
            ["noRecords"] = "Admin.Common.Grid.NoRecords",
            ["firstPage"] = "Admin.Common.Grid.FirstPage",
            ["previousPage"] = "Admin.Common.Grid.PreviousPage",
            ["nextPage"] = "Admin.Common.Grid.NextPage",
            ["lastPage"] = "Admin.Common.Grid.LastPage",
            ["toggleDetail"] = "Admin.Common.Grid.ToggleDetail",
            ["selectAll"] = "Admin.Common.Grid.SelectAll",
            ["selectRow"] = "Admin.Common.Grid.SelectRow"
        };
        var languageId = _contextAccessor.WorkContext?.WorkingLanguage?.Id;
        var texts = new Dictionary<string, string>();
        foreach (var (name, key) in keys)
        {
            //a resource missing from the database (e.g. a 2.4 development database that
            //never imported en_240.xml) is left out, so the grid shows its neutral default
            //("1 - 15 / 40", no label) instead of the raw resource key
            var value = languageId is null
                ? null
                : _translationService.GetResource(key, languageId, string.Empty, true);
            if (!string.IsNullOrEmpty(value)) texts[name] = value;
        }

        foreach (var (name, value) in custom)
            texts[name] = value;
        return texts;
    }
}

/// <summary>
///     Collects the configuration of a grid (or of its detail grid) from child tag helpers.
///     Templates are always collected on the root grid, which renders them.
/// </summary>
internal class GridBuilder
{
    private int _templateCount;

    public GridBuilder(GridDefinition grid, string id, GridBuilder root = null)
    {
        Grid = grid;
        Id = id;
        Root = root ?? this;
    }

    public GridDefinition Grid { get; }
    public string Id { get; }
    public GridBuilder Root { get; }
    public IDictionary<string, string> Texts { get; } = new Dictionary<string, string>();
    public IList<(string Id, IHtmlContent Html)> Templates { get; } = new List<(string, IHtmlContent)>();

    public string AddTemplate(IHtmlContent html)
    {
        var root = Root;
        var id = $"{root.Id}-template-{++root._templateCount}";
        root.Templates.Add((id, html));
        return id;
    }
}
