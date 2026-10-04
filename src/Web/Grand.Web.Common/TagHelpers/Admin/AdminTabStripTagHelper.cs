using Grand.Mediator;
using Grand.Web.Common.Events;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Json;
using Wangkanai.Detection.Models;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     Renders a tab strip as Bootstrap tab markup driven by admin.ui.js
///     (adminapp/src/ui/tabs.js), in place of the Kendo TabStrip the panels used.
///     The API is unchanged: selected-tab-index is posted and restored, and the global
///     tabstrip_on_tab_select / tabstrip_on_tab_show hooks still fire.
/// </summary>
[HtmlTargetElement("admin-tabstrip")]
public class AdminTabStripTagHelper : TagHelper
{
    private readonly IDetectionService _detectionService;
    private readonly IMediator _mediator;

    public AdminTabStripTagHelper(IMediator mediator, IDetectionService detectionService)
    {
        _mediator = mediator;
        _detectionService = detectionService;
    }

    [HtmlAttributeName("SetTabPos")] public bool SetTabPos { get; set; } = false;

    [HtmlAttributeName("BindGrid")] public bool BindGrid { get; set; } = false;

    [HtmlAttributeName("Name")] public string Name { get; set; }

    [ViewContext] public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName] = new List<string>();
        _ = await output.GetChildContentAsync();
        var list = (List<string>)ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName];
        if (_detectionService.Device.Type == Device.Mobile || _detectionService.Device.Type == Device.Tablet)
            SetTabPos = false;

        var selectedTabIndex = GetSelectedTabIndex();
        //nested strips number their tabs outside the page range (Product/ProductAttributes
        //starts at 100); their first pane is the active one
        var activeIndex = selectedTabIndex < list.Count ? selectedTabIndex : 0;

        output.TagName = "div";
        output.Attributes.SetAttribute("id", Name);
        //hidden until the runtime has activated a tab, so the panes never flash unstyled
        output.Attributes.SetAttribute("style", "display:none");
        output.Attributes.SetAttribute("class", SetTabPos ? "grand-tabstrip grand-tabstrip-left" : "grand-tabstrip");
        output.Attributes.SetAttribute("data-grand-tabstrip", JsonSerializer.Serialize(new {
            bindGrid = BindGrid,
            selectedIndex = activeIndex
        }));

        var content = new TagBuilder("div");
        content.AddCssClass("tab-content");
        for (var i = 0; i < list.Count; i++)
            content.InnerHtml.AppendHtml(Pane(list[i], i == activeIndex));
        output.PostContent.AppendHtml(content);

        //Blocks contributed by plugins used to be interpolated into a JavaScript string
        //literal, which broke on an apostrophe and let markup from a plugin run as script.
        //They are markup now, handed to the runtime in a <template> the same way
        //<admin-tab-append> does it.
        var eventMessage = new AdminTabStripCreated(Name);
        await _mediator.Publish(eventMessage);
        foreach (var (tabName, blockContent) in eventMessage.BlocksToRender)
            output.PostElement.AppendHtml(AdminTabAppendTagHelper.Template(Name, tabName, blockContent));

        output.PreElement.AppendHtml(SelectedTabIndexInput(selectedTabIndex));
    }

    internal static TagBuilder Pane(IHtmlContent content, bool active)
    {
        var pane = new TagBuilder("div");
        pane.AddCssClass("tab-pane");
        pane.Attributes["role"] = "tabpanel";
        pane.Attributes["aria-hidden"] = active ? "false" : "true";
        if (active)
        {
            pane.AddCssClass("active");
            pane.AddCssClass("show");
            //the class admin.common.js selects on when it loads the grids of a restored tab
            pane.AddCssClass("k-state-active");
        }

        pane.InnerHtml.AppendHtml(content);
        return pane;
    }

    internal static TagBuilder Pane(string html, bool active)
    {
        return Pane(new HtmlString(html), active);
    }

    internal static TagBuilder SelectedTabIndexInput(int selectedTabIndex)
    {
        var input = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
        input.Attributes["type"] = "hidden";
        input.Attributes["id"] = "selected-tab-index";
        input.Attributes["name"] = "selected-tab-index";
        input.Attributes["value"] = selectedTabIndex.ToString();
        return input;
    }

    private int GetSelectedTabIndex()
    {
        var index = 0;
        var dataKey = "Grand.selected-tab-index";
        if (ViewContext.ViewData[dataKey] is int) index = (int)ViewContext.ViewData[dataKey];
        if (ViewContext.TempData[dataKey] is int) index = (int)ViewContext.TempData[dataKey];

        //ensure it's not negative
        if (index < 0)
            index = 0;

        return index;
    }
}
