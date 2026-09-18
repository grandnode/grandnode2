using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.TagHelpers.Admin.Grid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The command column: Edit (Update/Cancel while editing) and Delete, plus
///     &lt;grid-command&gt; buttons calling global functions with (dataItem, event, grid).
/// </summary>
[HtmlTargetElement("grid-commands", ParentTag = "admin-grid")]
[HtmlTargetElement("grid-commands", ParentTag = "grid-detail")]
[RestrictChildren("grid-command")]
public class GridCommandsTagHelper : TagHelper
{
    public bool Edit { get; set; }
    public bool Destroy { get; set; }
    public int? Width { get; set; }
    public string Title { get; set; }

    /// <summary>
    ///     Condition (admin.grid.js expression syntax) that shows Edit and Delete for a row,
    ///     e.g. "StoreId == '' || StoreId == 'abc'". The server still checks ownership.
    ///     <see cref="EditVisibleIf" /> and <see cref="DestroyVisibleIf" /> apply on top of it.
    /// </summary>
    public string VisibleIf { get; set; }

    /// <summary>
    ///     Condition that shows Edit for a row, on top of <see cref="VisibleIf" />, when Edit and
    ///     Delete follow different rules.
    /// </summary>
    public string EditVisibleIf { get; set; }

    /// <summary>Condition that shows Delete for a row, on top of <see cref="VisibleIf" />.</summary>
    public string DestroyVisibleIf { get; set; }

    /// <summary>
    ///     Text shown (muted, small) in place of the buttons of a row that got none, so an empty
    ///     cell does not look like something failed to load.
    /// </summary>
    public string EmptyText { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        var commands = new GridCommands {
            Edit = Edit,
            Destroy = Destroy,
            Width = Width,
            Title = Title,
            VisibleIf = VisibleIf,
            EditVisibleIf = string.IsNullOrWhiteSpace(EditVisibleIf) ? null : EditVisibleIf,
            DestroyVisibleIf = string.IsNullOrWhiteSpace(DestroyVisibleIf) ? null : DestroyVisibleIf,
            EmptyText = string.IsNullOrEmpty(EmptyText) ? null : EmptyText
        };
        context.Items[typeof(GridCommands)] = commands;
        await output.GetChildContentAsync();
        builder.Grid.Commands = commands;
        output.SuppressOutput();
    }
}

[HtmlTargetElement("grid-command", ParentTag = "grid-commands", TagStructure = TagStructure.WithoutEndTag)]
public class GridCommandTagHelper : TagHelper
{
    public string Name { get; set; }
    public string Text { get; set; }

    /// <summary>Global function called with (dataItem, event, grid).</summary>
    public string Click { get; set; }

    public string Icon { get; set; }

    [HtmlAttributeName("class")] public string ClassName { get; set; }

    public string VisibleIf { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var commands = (GridCommands)context.Items[typeof(GridCommands)];
        commands.Custom ??= new List<GridCommand>();
        commands.Custom.Add(new GridCommand {
            Name = Name,
            Text = Text,
            Click = Click,
            Icon = Icon,
            ClassName = ClassName,
            VisibleIf = VisibleIf
        });
        output.SuppressOutput();
    }
}

/// <summary>Toolbar button that adds a row in inline edit mode (Kendo toolbar: ["create"]).</summary>
[HtmlTargetElement("grid-toolbar-create", ParentTag = "admin-grid", TagStructure = TagStructure.WithoutEndTag)]
public class GridToolbarCreateTagHelper : TagHelper
{
    private readonly ITranslationService _translationService;

    public GridToolbarCreateTagHelper(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    /// <summary>Button text; defaults to Admin.Common.AddNewRecord.</summary>
    public string Text { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        builder.Grid.Toolbar ??= new GridToolbar();
        builder.Grid.Toolbar.Create = string.IsNullOrEmpty(Text) ? _translationService.GetResource("Admin.Common.AddNewRecord") : Text;
        output.SuppressOutput();
    }
}

/// <summary>Toolbar button that sends the changed rows of edit-mode="Batch" (Kendo toolbar: ["save"]).</summary>
[HtmlTargetElement("grid-toolbar-save", ParentTag = "admin-grid", TagStructure = TagStructure.WithoutEndTag)]
public class GridToolbarSaveTagHelper : TagHelper
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;

    public GridToolbarSaveTagHelper(ITranslationService translationService, IContextAccessor contextAccessor)
    {
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>Button text; defaults to Admin.Common.SaveChanges.</summary>
    public string Text { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        builder.Grid.Toolbar ??= new GridToolbar();
        builder.Grid.Toolbar.Save = string.IsNullOrEmpty(Text)
            ? GridToolbarText.Get(_translationService, _contextAccessor, "Admin.Common.SaveChanges", "Save changes")
            : Text;
        output.SuppressOutput();
    }
}

/// <summary>Toolbar button that restores the rows of edit-mode="Batch" (Kendo toolbar: ["cancel"]).</summary>
[HtmlTargetElement("grid-toolbar-cancel", ParentTag = "admin-grid", TagStructure = TagStructure.WithoutEndTag)]
public class GridToolbarCancelTagHelper : TagHelper
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;

    public GridToolbarCancelTagHelper(ITranslationService translationService, IContextAccessor contextAccessor)
    {
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>Button text; defaults to Admin.Common.Grid.CancelChanges.</summary>
    public string Text { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        builder.Grid.Toolbar ??= new GridToolbar();
        builder.Grid.Toolbar.Cancel = string.IsNullOrEmpty(Text)
            ? GridToolbarText.Get(_translationService, _contextAccessor, "Admin.Common.Grid.CancelChanges", "Cancel changes")
            : Text;
        output.SuppressOutput();
    }
}

internal static class GridToolbarText
{
    /// <summary>
    ///     The resource in the working language, or the Kendo default text when the resource is
    ///     missing (a database that never imported the upgrade resources), never the raw key.
    /// </summary>
    public static string Get(ITranslationService translationService, IContextAccessor contextAccessor, string key, string fallback)
    {
        var languageId = contextAccessor.WorkContext?.WorkingLanguage?.Id;
        var value = languageId is null ? null : translationService.GetResource(key, languageId, string.Empty, true);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}

/// <summary>A localized text for cell templates, read there as {{ $texts.Name }}.</summary>
[HtmlTargetElement("grid-text", ParentTag = "admin-grid", Attributes = "name", TagStructure = TagStructure.WithoutEndTag)]
public class GridTextTagHelper : TagHelper
{
    public string Name { get; set; }
    public string Value { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        builder.Texts[Name] = Value ?? string.Empty;
        output.SuppressOutput();
    }
}

/// <summary>
///     Detail rows: each master row gets an expander that loads a nested grid from
///     read-url with query parameters taken from the master row
///     (param="customerId:CustomerId", comma separated for several).
/// </summary>
[HtmlTargetElement("grid-detail", ParentTag = "admin-grid")]
[RestrictChildren("grid-column", "grid-commands")]
public class GridDetailTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public GridDetailTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    public string ReadUrl { get; set; }

    /// <summary>Destroy URL of the detail rows; the master row parameters are appended like for read.</summary>
    public string DestroyUrl { get; set; }

    public string Controller { get; set; }
    public string ReadAction { get; set; }
    public string Param { get; set; }
    public string Key { get; set; }
    public GridPager Pager { get; set; } = GridPager.Compact;
    public int? PageSize { get; set; }

    /// <summary>Asks before a detail row is deleted.</summary>
    public bool ConfirmDestroy { get; set; }

    /// <summary>Condition (admin.grid.js expression syntax) on the master row that shows its expander.</summary>
    public string VisibleIf { get; set; }

    /// <summary>Global function called after each detail grid rendered its rows.</summary>
    public string OnDataBound { get; set; }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var parent = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        var detail = new GridDetailDefinition {
            Key = Key,
            Pager = Pager,
            PageSize = PageSize,
            Params = ParseParams(Param),
            Transport = new GridTransport { Read = ResolveUrl(), Destroy = string.IsNullOrEmpty(DestroyUrl) ? null : DestroyUrl },
            ConfirmDestroy = ConfirmDestroy ? true : null,
            VisibleIf = string.IsNullOrWhiteSpace(VisibleIf) ? null : VisibleIf,
            Events = string.IsNullOrWhiteSpace(OnDataBound) ? null : new Dictionary<string, string> { ["dataBound"] = OnDataBound }
        };
        context.Items[AdminGridTagHelper.ContextKey] = new GridBuilder(detail, parent.Id, parent.Root);
        await output.GetChildContentAsync();
        parent.Grid.Detail = detail;
        output.SuppressOutput();
    }

    internal static IList<GridDetailParam> ParseParams(string param)
    {
        if (string.IsNullOrWhiteSpace(param))
            return null;
        return param.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split(':', 2, StringSplitOptions.TrimEntries))
            .Select(parts => new GridDetailParam { Name = parts[0], Field = parts.Length > 1 ? parts[1] : parts[0] })
            .ToList();
    }

    private string ResolveUrl()
    {
        if (!string.IsNullOrEmpty(ReadUrl) || string.IsNullOrEmpty(ReadAction))
            return ReadUrl;
        var area = ViewContext.RouteData.Values["area"]?.ToString();
        return _urlHelperFactory.GetUrlHelper(ViewContext).Action(ReadAction, Controller, new { area });
    }
}
