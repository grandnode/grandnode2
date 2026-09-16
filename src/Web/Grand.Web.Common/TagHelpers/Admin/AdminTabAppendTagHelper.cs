using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     Adds a tab to a tab strip from outside the view that declares it (plugins).
///     The tab is rendered as a &lt;template&gt; the tab strip runtime consumes
///     (adminapp/src/ui/tabs.js), instead of the Kendo append() call the content used to be
///     interpolated into as a JavaScript string literal.
/// </summary>
[HtmlTargetElement("admin-tab-append", Attributes = "tab-strip-name, tab-name, tab-content")]
public class AdminTabAppendTagHelper : TagHelper
{
    private const string TabStripName = "tab-strip-name";
    private const string TabName = "tab-name";
    private const string TabContent = "tab-content";


    [HtmlAttributeName(TabName)] public string Name { get; set; }

    [HtmlAttributeName(TabContent)] public string Content { get; set; }

    [HtmlAttributeName(TabStripName)] public string StripName { get; set; }

    /// <summary>The template a tab strip picks up for one appended tab.</summary>
    internal static TagBuilder Template(string stripName, string tabName, IHtmlContent content)
    {
        var template = new TagBuilder("template");
        //the attribute value is HTML-encoded by TagBuilder, so a quote in a plugin title
        //can no longer end the attribute, and the content is markup instead of script
        template.Attributes["data-grand-tab-append"] = stripName;
        template.Attributes["data-tab-name"] = tabName;
        template.InnerHtml.AppendHtml(content);
        return template;
    }

    public override void Process(TagHelperContext tagHelperContext, TagHelperOutput output)
    {
        output.TagName = null;
        output.Content.SetHtmlContent(Template(StripName, Name, new HtmlString(Content)));
    }
}
