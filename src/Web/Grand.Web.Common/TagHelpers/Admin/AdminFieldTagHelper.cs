using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.TagHelpers.Admin.Extend;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Reflection;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     One form field: its label, its control, its hint and its validation message.
///     &lt;admin-field asp-for="Sku"/&gt; replaces the five-element house row
///     (<c>div.mb-3 &gt; admin-label + div.col-md-9.col-sm-9 &gt; control + span[asp-validation-for]</c>),
///     which the panels repeat some 1590 times.
///     The label sits above the control, so the 3/9 grid is gone and two fields can share a row
///     inside <c>.grand-fields--2col</c>. Child content, when given, is used as the control - that
///     covers input groups and anything the metadata cannot build.
/// </summary>
[HtmlTargetElement("admin-field", Attributes = ForAttributeName)]
public class AdminFieldTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    private readonly IContextAccessor _contextAccessor;
    private readonly IHtmlHelper _htmlHelper;
    private readonly ITranslationService _translationService;

    public AdminFieldTagHelper(IHtmlHelper htmlHelper, IContextAccessor contextAccessor,
        ITranslationService translationService)
    {
        _htmlHelper = htmlHelper;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
    }

    [HtmlAttributeName(ForAttributeName)] public ModelExpression For { get; set; }

    /// <summary>
    ///     The property whose display name (and hint) labels the field, when it is not the bound
    ///     one: a select bound to ProductAttributeId is labelled by ProductAttribute, which is the
    ///     property that carries the resource.
    /// </summary>
    [HtmlAttributeName("asp-label-for")]
    public ModelExpression LabelFor { get; set; }

    /// <summary>The control's width: sm, md, lg, or full (the default).</summary>
    [HtmlAttributeName("size")]
    public string Size { get; set; }

    /// <summary>Marks the label with an asterisk.</summary>
    [HtmlAttributeName("asp-required")]
    public bool IsRequired { get; set; }

    /// <summary>The options of a select, as &lt;admin-select asp-items&gt; takes them. An
    /// IEnumerable, because a SelectList - what EnumTranslationService.ToSelectList returns -
    /// is not an IList.</summary>
    [HtmlAttributeName("asp-items")]
    public IEnumerable<SelectListItem> Items { get; set; }

    /// <summary>An editor template name, when the metadata's own choice is not wanted.</summary>
    [HtmlAttributeName("asp-template")]
    public string Template { get; set; }

    /// <summary>Text under the control, in place of the field's <c>.Hint</c> resource.</summary>
    [HtmlAttributeName("hint")]
    public string Hint { get; set; }

    /// <summary>
    ///     The unit the value is in, shown at the end of the control: a currency code, kg, %. The
    ///     views used to write it as bare text after the input, where it read as part of the next
    ///     field.
    /// </summary>
    [HtmlAttributeName("suffix")]
    public string Suffix { get; set; }

    [HtmlAttributeNotBound] [ViewContext] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var child = await output.GetChildContentAsync();

        (_htmlHelper as IViewContextAware)?.Contextualize(ViewContext);

        var isCheck = For.Metadata.UnderlyingOrModelType == typeof(bool);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("grand-field", HtmlEncoder.Default);
        if (isCheck)
        {
            //a Bootstrap switch, which is what a yes/no answer reads as; the framework's
            //default checkbox template renders the pre-migration .check-box class, whose look
            //depended on a .control__indicator sibling the views hand-wrote 1571 times
            output.AddClass("grand-field--check", HtmlEncoder.Default);
            output.AddClass("form-check", HtmlEncoder.Default);
            output.AddClass("form-switch", HtmlEncoder.Default);
        }
        else if (!string.IsNullOrEmpty(Size) && !Size.Equals("full", StringComparison.OrdinalIgnoreCase))
            output.AddClass($"grand-field--{Size.ToLowerInvariant()}", HtmlEncoder.Default);

        var content = new DefaultTagHelperContent();
        content.AppendHtml(Label());

        var control = new TagBuilder("div");
        control.AddCssClass("grand-field__control");
        if (!string.IsNullOrEmpty(Suffix)) control.AddCssClass("input-group");
        control.InnerHtml.AppendHtml(child.IsEmptyOrWhiteSpace ? await BuildControl() : child);
        if (!string.IsNullOrEmpty(Suffix))
        {
            var suffix = new TagBuilder("span");
            suffix.AddCssClass("input-group-text");
            suffix.InnerHtml.Append(Suffix);
            control.InnerHtml.AppendHtml(suffix);
        }

        content.AppendHtml(control);

        var hint = HintText();
        if (!string.IsNullOrEmpty(hint))
        {
            var hintElement = new TagBuilder("div");
            hintElement.AddCssClass("grand-field__hint");
            hintElement.InnerHtml.Append(hint);
            content.AppendHtml(hintElement);
        }

        content.AppendHtml(_htmlHelper.ValidationMessage(For.Name));
        output.Content.SetHtmlContent(content);
    }

    private IHtmlContent Label()
    {
        var label = new TagBuilder("label");
        label.AddCssClass("grand-field__label");
        if (IsRequired) label.AddCssClass("required");
        var id = For.Name?.Replace('.', '_').Replace('[', '_').Replace(']', '_');
        if (!string.IsNullOrEmpty(id)) label.Attributes["for"] = id;
        label.InnerHtml.Append(Text());
        return label;
    }

    /// <summary>The display name resolved as a resource, the way admin-label does it.</summary>
    private string Text()
    {
        var displayName = (LabelFor ?? For).Metadata.GetDisplayName() ?? string.Empty;
        return AdminText.Resource(_translationService, _contextAccessor, displayName.ToLowerInvariant(), displayName);
    }

    /// <summary>The hint given on the element, else the field's <c>.Hint</c> resource.</summary>
    private string HintText()
    {
        if (!string.IsNullOrEmpty(Hint)) return Hint;
        var displayName = (LabelFor ?? For).Metadata.GetDisplayName();
        return string.IsNullOrEmpty(displayName)
            ? null
            : AdminText.Resource(_translationService, _contextAccessor, displayName + ".Hint", null);
    }

    /// <summary>
    ///     The control the model metadata asks for, built through the same editor templates
    ///     &lt;admin-input&gt; uses, so a date stays a date picker and a number stays a numeric box.
    /// </summary>
    private async Task<IHtmlContent> BuildControl()
    {
        if (For.Metadata.UnderlyingOrModelType == typeof(bool))
            return _htmlHelper.CheckBox(For.Name, For.Model as bool? ?? false,
                new { @class = "form-check-input", role = "switch" });

        //asp-items means a plain select, as <admin-select> renders one; the editor templates
        //have no notion of a list and would hand back a text box
        if (Items != null)
            return _htmlHelper.DropDownList(For.Name, Items, new { @class = "form-select" });

        var viewEngine = GetPrivateFieldValue(_htmlHelper, "_viewEngine") as IViewEngine;
        var bufferScope = GetPrivateFieldValue(_htmlHelper, "_bufferScope") as IViewBufferScope;

        var isString = For.Metadata.ModelType == typeof(string);
        object htmlAttributes = isString ? new { @class = "form-control" } : null;

        var templateBuilder = new TemplateBuilder(
            viewEngine,
            bufferScope,
            _htmlHelper.ViewContext,
            _htmlHelper.ViewData,
            For.ModelExplorer,
            For.Name,
            Template,
            false,
            new { htmlAttributes, postfix = (string)null });

        return await templateBuilder.Build();
    }

    private static object GetPrivateFieldValue(object target, string fieldName)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(target);
            type = type.BaseType;
        }

        return null;
    }
}
