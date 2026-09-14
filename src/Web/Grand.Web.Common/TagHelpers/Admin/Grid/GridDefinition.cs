using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grand.Web.Common.TagHelpers.Admin.Grid;

public enum GridEditMode
{
    None,
    Inline,

    /// <summary>Cells edit in place; the toolbar Save changes button sends the changed rows (Kendo incell).</summary>
    Batch
}

public enum GridPager
{
    Full,
    Compact,
    None
}

public enum GridSelectable
{
    None,
    Checkbox,

    /// <summary>One row selected by clicking it (Kendo selectable: true).</summary>
    Row
}

public enum GridEditor
{
    None,
    Text,
    Numeric,
    Integer,
    Checkbox,
    Date,
    DateTime,
    Select,
    Custom
}

public enum GridAlign
{
    /// <summary>Inherits the table direction; not serialized.</summary>
    Default,
    Left,
    Center,
    Right
}

/// <summary>
///     Configuration of one grid (or detail grid) emitted as the data-grand-grid JSON that
///     admin.grid.js reads. Property names are the JSON contract with adminapp/src/grid.
/// </summary>
public class GridDefinition
{
    public string Id { get; set; }
    public string Key { get; set; }
    public GridTransport Transport { get; set; } = new();
    public int? PageSize { get; set; }
    public IList<int> PageSizes { get; set; }
    public GridPager Pager { get; set; } = GridPager.Full;
    public bool? AutoBind { get; set; }
    public GridEditMode EditMode { get; set; } = GridEditMode.None;
    public bool? ReloadAfterSave { get; set; }
    public bool? ConfirmDestroy { get; set; }
    public GridSelectable Selectable { get; set; } = GridSelectable.None;
    public string SelectionName { get; set; }
    public string BatchPrefix { get; set; }

    /// <summary>Local rows for a grid without a read URL, serialized with their own property names.</summary>
    public JsonElement? Data { get; set; }

    public string AdditionalData { get; set; }
    public string SearchForm { get; set; }
    public GridToolbar Toolbar { get; set; }
    public IList<GridColumnDefinition> Columns { get; set; } = new List<GridColumnDefinition>();
    public GridCommands Commands { get; set; }
    public GridDetailDefinition Detail { get; set; }
    public IDictionary<string, string> Events { get; set; }
    public IDictionary<string, string> Texts { get; set; }
    public GridCulture Culture { get; set; }
    public bool? Rtl { get; set; }

    internal static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        //the JSON ends up in an attribute value, which Razor HTML-encodes
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }
}

public class GridTransport
{
    public string Read { get; set; }
    public string Create { get; set; }
    public string Update { get; set; }
    public string Destroy { get; set; }
}

public class GridToolbar
{
    public string Create { get; set; }
    public string Save { get; set; }
    public string Cancel { get; set; }
}

public class GridColumnDefinition
{
    public string Field { get; set; }
    public string Title { get; set; }
    public int? Width { get; set; }
    public string Format { get; set; }
    public GridEditor? Editor { get; set; }
    public string EditorName { get; set; }
    public bool? Editable { get; set; }
    public bool? Required { get; set; }
    public int? Decimals { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public int? MaxLength { get; set; }
    public IList<GridOption> Options { get; set; }
    public string OptionsUrl { get; set; }
    public string OptionTextField { get; set; }
    public string OptionValueField { get; set; }
    public string OptionLabel { get; set; }
    public string OptionsFilter { get; set; }
    public string TextField { get; set; }
    public string DefaultValue { get; set; }
    public GridAlign? Align { get; set; }
    public GridAlign? HeaderAlign { get; set; }
    public int? MinScreenWidth { get; set; }
    public bool? Hidden { get; set; }
    public bool? Encoded { get; set; }
    public string CssClass { get; set; }
    public string Template { get; set; }
}

public class GridOption
{
    public string Value { get; set; }
    public string Text { get; set; }
}

public class GridCommands
{
    public bool Edit { get; set; }
    public bool Destroy { get; set; }
    public int? Width { get; set; }
    public string Title { get; set; }
    public string VisibleIf { get; set; }
    public IList<GridCommand> Custom { get; set; }
}

public class GridCommand
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Click { get; set; }
    public string Icon { get; set; }
    public string ClassName { get; set; }
    public string VisibleIf { get; set; }
}

public class GridDetailDefinition : GridDefinition
{
    public IList<GridDetailParam> Params { get; set; }

    /// <summary>Condition (admin.grid.js expression syntax) that shows the expander of a master row.</summary>
    public string VisibleIf { get; set; }
}

public class GridDetailParam
{
    public string Name { get; set; }
    public string Field { get; set; }
}
