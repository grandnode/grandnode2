using Grand.Web.Common.TagHelpers.Admin.Grid;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     A column of an &lt;admin-grid&gt; or of its &lt;grid-detail&gt;. Wrap it in @if for
///     conditional columns. Titles and option texts are data (JSON), never template markup.
/// </summary>
[HtmlTargetElement("grid-column", ParentTag = "admin-grid")]
[HtmlTargetElement("grid-column", ParentTag = "grid-detail")]
[RestrictChildren("cell-template")]
public class GridColumnTagHelper : TagHelper
{
    internal static readonly object ContextKey = typeof(GridColumnDefinition);

    public string Field { get; set; }
    public string Title { get; set; }
    public int? Width { get; set; }

    /// <summary>Kendo-style format: {0:G}, {0:0}, {0:n2}, n8, c2, HH:mm.</summary>
    public string Format { get; set; }

    /// <summary>Inline editor; None (default) leaves the column read-only in edit mode.</summary>
    /// <remarks>Not nullable: Razor only accepts bare enum names (editor="Numeric") for enum properties.</remarks>
    public GridEditor Editor { get; set; } = GridEditor.None;

    /// <summary>Name registered with GrandAdmin.grids.editors.register for editor="Custom".</summary>
    public string EditorName { get; set; }

    public bool Editable { get; set; } = true;
    public bool Required { get; set; }
    public int? Decimals { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public int? MaxLength { get; set; }

    /// <summary>Options of a Select editor; the display text is looked up from them too.</summary>
    public IEnumerable<SelectListItem> Options { get; set; }

    /// <summary>Remote options of a Select editor: JSON array or DataSourceResult, loaded with GET.</summary>
    public string OptionsUrl { get; set; }

    public string OptionTextField { get; set; }
    public string OptionValueField { get; set; }
    public string OptionLabel { get; set; }

    /// <summary>
    ///     Remote Select only: what is typed in the cell reloads options-url filtered on the option
    ///     text field with this operator (startswith, contains), the query a Kendo DropDownList
    ///     with serverFiltering sends. Without it the list is read once and searched in the browser.
    /// </summary>
    public string OptionsFilter { get; set; }

    /// <summary>Field of the row holding the display text of a Select value (e.g. StoreName for StoreId).</summary>
    public string TextField { get; set; }

    public string DefaultValue { get; set; }
    public GridAlign Align { get; set; } = GridAlign.Default;
    public GridAlign HeaderAlign { get; set; } = GridAlign.Default;

    /// <summary>Hides the column below this window width, like Kendo minScreenWidth.</summary>
    public int? MinScreenWidth { get; set; }

    public bool Hidden { get; set; }

    /// <summary>false renders the field value as HTML (Kendo encoded: false). Server-built markup only.</summary>
    public bool Encoded { get; set; } = true;

    public string CssClass { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        var column = new GridColumnDefinition {
            Field = Field,
            Title = Title,
            Width = Width,
            Format = Format,
            Editor = Editor == GridEditor.None ? null : Editor,
            EditorName = EditorName,
            Editable = Editable ? null : false,
            Required = Required ? true : null,
            Decimals = Decimals,
            Min = Min,
            Max = Max,
            MaxLength = MaxLength,
            Options = Options?.Select(x => new GridOption { Value = x.Value ?? x.Text, Text = x.Text }).ToList(),
            OptionsUrl = OptionsUrl,
            OptionTextField = OptionTextField,
            OptionValueField = OptionValueField,
            OptionLabel = OptionLabel,
            OptionsFilter = string.IsNullOrWhiteSpace(OptionsFilter) ? null : OptionsFilter,
            TextField = TextField,
            DefaultValue = DefaultValue,
            Align = Align == GridAlign.Default ? null : Align,
            HeaderAlign = HeaderAlign == GridAlign.Default ? null : HeaderAlign,
            MinScreenWidth = MinScreenWidth,
            Hidden = Hidden ? true : null,
            Encoded = Encoded ? null : false,
            CssClass = CssClass
        };
        context.Items[ContextKey] = column;
        await output.GetChildContentAsync();
        builder.Grid.Columns.Add(column);
        output.SuppressOutput();
    }
}

/// <summary>
///     Markup of a cell, rendered by admin.grid.js from an inert &lt;template&gt;:
///     {{ Field }} encoded text, {{ Field | n2 }} formatted, {{{ Field }}} raw HTML (opt-in),
///     {{ $texts.Name }} a &lt;grid-text&gt;, data-if / data-else conditions and
///     data-grid-click="globalFunction". No JavaScript is evaluated.
/// </summary>
[HtmlTargetElement("cell-template", ParentTag = "grid-column")]
public class GridCellTemplateTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var builder = (GridBuilder)context.Items[AdminGridTagHelper.ContextKey];
        var column = (GridColumnDefinition)context.Items[GridColumnTagHelper.ContextKey];
        var content = await output.GetChildContentAsync();
        column.Template = builder.AddTemplate(content);
        output.SuppressOutput();
    }
}
