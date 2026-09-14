using Grand.Business.Core.Interfaces.Common.Localization;
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
    /// </summary>
    public string VisibleIf { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        var commands = new GridCommands {
            Edit = Edit,
            Destroy = Destroy,
            Width = Width,
            Title = Title,
            VisibleIf = VisibleIf
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
        builder.Grid.Toolbar = new GridToolbar {
            Create = string.IsNullOrEmpty(Text) ? _translationService.GetResource("Admin.Common.AddNewRecord") : Text
        };
        output.SuppressOutput();
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
    public string Controller { get; set; }
    public string ReadAction { get; set; }
    public string Param { get; set; }
    public string Key { get; set; }
    public GridPager Pager { get; set; } = GridPager.Compact;
    public int? PageSize { get; set; }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var parent = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        var detail = new GridDetailDefinition {
            Key = Key,
            Pager = Pager,
            PageSize = PageSize,
            Params = ParseParams(Param),
            Transport = new GridTransport { Read = ResolveUrl() }
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
