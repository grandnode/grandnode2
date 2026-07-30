using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers;

/// <summary>
///     Server-rendered Bootstrap 5 tabs.
///     <para>
///         Bootstrap needs the tab buttons emitted before the panes, while views want to
///         keep a tab's title next to its content (and wrap whole tabs in Razor
///         conditionals). These two helpers bridge that: each &lt;gn-tab&gt; captures its
///         own content, and &lt;gn-tabs&gt; emits the nav list and the panes afterwards.
///     </para>
/// </summary>
[HtmlTargetElement("gn-tabs")]
public class TabsTagHelper : TagHelper
{
    internal const string ItemsKey = "Grand.Web.Common.Tabs.Items";
    private const string CounterKey = "Grand.Web.Common.Tabs.Counter";

    [HtmlAttributeName("content-class")] public string ContentClass { get; set; }

    /// <summary>center | end (BootstrapVue also accepted "right")</summary>
    [HtmlAttributeName("align")]
    public string Align { get; set; }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // a nested tab strip must not steal the outer one's items
        var outer = ViewContext.ViewData[ItemsKey];
        var items = new List<TabItem>();
        ViewContext.ViewData[ItemsKey] = items;
        await output.GetChildContentAsync();
        ViewContext.ViewData[ItemsKey] = outer;

        if (items.Count > 0 && !items.Any(x => x.Active))
            items[0].Active = true;

        var strip = NextStripIndex();
        var nav = new StringBuilder();
        var panes = new StringBuilder();

        nav.Append($"<ul class=\"nav nav-tabs{AlignClass()}\" role=\"tablist\">");
        panes.Append($"<div class=\"tab-content{Class(ContentClass)}\">");

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var paneId = string.IsNullOrEmpty(item.Id) ? $"tabpane-{strip}-{i}" : item.Id;
            var buttonId = $"{paneId}-tab";
            var active = item.Active ? " active" : "";
            var onClick = string.IsNullOrEmpty(item.OnClick)
                ? ""
                : $" @click=\"{HtmlEncoder.Default.Encode(item.OnClick)}\"";

            nav.Append(
                $"<li class=\"nav-item\" role=\"presentation\">" +
                $"<button type=\"button\" id=\"{buttonId}\" class=\"nav-link{active}\" " +
                $"data-bs-toggle=\"tab\" data-bs-target=\"#{paneId}\" role=\"tab\" " +
                $"aria-controls=\"{paneId}\" aria-selected=\"{(item.Active ? "true" : "false")}\"{onClick}>" +
                $"{HtmlEncoder.Default.Encode(item.Title ?? "")}</button></li>");

            panes.Append(
                $"<div class=\"tab-pane fade{(item.Active ? " show active" : "")}\" id=\"{paneId}\" " +
                $"role=\"tabpanel\" aria-labelledby=\"{buttonId}\">{item.Content}</div>");
        }

        nav.Append("</ul>");
        panes.Append("</div>");

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        var authored = output.Attributes["class"]?.Value?.ToString();
        output.Attributes.SetAttribute("class",
            string.IsNullOrEmpty(authored) ? "tabs" : "tabs " + authored);
        output.Content.SetHtmlContent(nav + panes.ToString());
    }

    private string AlignClass()
    {
        return Align switch {
            "center" => " justify-content-center",
            "end" or "right" => " justify-content-end",
            _ => ""
        };
    }

    private static string Class(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : " " + value;
    }

    private int NextStripIndex()
    {
        var index = ViewContext.ViewData[CounterKey] as int? ?? 0;
        ViewContext.ViewData[CounterKey] = index + 1;
        return index;
    }
}

[HtmlTargetElement("gn-tab", ParentTag = "gn-tabs")]
public class TabTagHelper : TagHelper
{
    [HtmlAttributeName("title")] public string Title { get; set; }

    /// <summary>Marks the initially selected tab; the first tab wins when none is set.</summary>
    [HtmlAttributeName("active")]
    public bool Active { get; set; }

    /// <summary>Id for the pane; the nav button gets "&lt;id&gt;-tab".</summary>
    [HtmlAttributeName("id")]
    public string Id { get; set; }

    /// <summary>Vue expression bound to the nav button's click, e.g. "askquestion.getCaptcha()".</summary>
    [HtmlAttributeName("on-click")]
    public string OnClick { get; set; }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();
        if (ViewContext.ViewData[TabsTagHelper.ItemsKey] is List<TabItem> items)
            items.Add(new TabItem {
                Title = Title,
                Active = Active,
                Id = Id,
                OnClick = OnClick,
                Content = content.GetContent()
            });
        output.SuppressOutput();
    }
}

internal class TabItem
{
    public string Title { get; set; }
    public bool Active { get; set; }
    public string Id { get; set; }
    public string OnClick { get; set; }
    public string Content { get; set; }
}
