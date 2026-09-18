using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.TagHelpers;

public class LocalizedEditorTagHelper : TagHelper
{
    [HtmlAttributeName("name")] public string Name { get; set; }

    [HtmlAttributeName("ignore-if-severa-stores")]
    public bool IgnoreIfSeveralStores { get; set; } = false;

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    [HtmlAttributeName("localized-template")]
    public Func<int, HelperResult> LocalizedTemplate { get; set; }

    [HtmlAttributeName("language-ids")] public List<string> LanguagesIds { get; set; }

    private static TagBuilder Item(IHtmlContent label, bool active)
    {
        var item = new TagBuilder("li");
        item.AddCssClass(active ? "nav-item active k-state-active" : "nav-item");
        item.Attributes["role"] = "presentation";
        var link = new TagBuilder("a");
        link.AddCssClass(active ? "nav-link active" : "nav-link");
        link.Attributes["href"] = "#";
        link.Attributes["role"] = "tab";
        link.Attributes["aria-selected"] = active ? "true" : "false";
        link.Attributes["tabindex"] = active ? "0" : "-1";
        link.InnerHtml.AppendHtml(label);
        item.InnerHtml.AppendHtml(link);
        return item;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var localizationSupported = LanguagesIds.Count > 1;
        var standardContent = (await output.GetChildContentAsync()).GetContent();
        if (IgnoreIfSeveralStores)
        {
            var storeService = ViewContext.HttpContext.RequestServices.GetRequiredService<IStoreService>();
            if ((await storeService.GetAllStores()).Count >= 2) localizationSupported = false;
        }

        if (localizationSupported)
        {
            //the same Bootstrap tab markup the <admin-tabstrip> tag helper renders, driven by
            //admin.ui.js (adminapp/src/ui/tabs.js) instead of the Kendo TabStrip
            var strip = new TagBuilder("div");
            strip.Attributes["id"] = Name;
            strip.Attributes["style"] = "display:none";
            //grand-tabstrip-localized: a language strip is drawn one step down from a page's
            //tab strip (ui.css), wherever it sits
            strip.AddCssClass("grand-tabstrip grand-tabstrip-localized");
            strip.Attributes["data-grand-tabstrip"] = "{\"bindGrid\":false,\"selectedIndex\":0}";

            var items = new TagBuilder("ul");
            items.AddCssClass("nav nav-tabs");
            items.Attributes["role"] = "tablist";
            items.InnerHtml.AppendHtml(Item(new HtmlString("Standard"), true));

            var languageService = ViewContext.HttpContext.RequestServices.GetRequiredService<ILanguageService>();
            var urlHelper = new UrlHelper(ViewContext);
            foreach (var locale in LanguagesIds)
            {
                var language = await languageService.GetLanguageById(locale);
                var label = new HtmlContentBuilder();
                var icon = new TagBuilder("img") { TagRenderMode = TagRenderMode.SelfClosing };
                icon.AddCssClass("k-image");
                icon.Attributes["alt"] = "";
                icon.Attributes["src"] = urlHelper.Content("~/assets/images/flags/" + language.FlagImageFileName);
                label.AppendHtml(icon);
                label.Append(" ");
                label.Append(language.Name);
                items.InnerHtml.AppendHtml(Item(label, false));
            }

            strip.InnerHtml.AppendHtml(items);

            var content = new TagBuilder("div");
            content.AddCssClass("tab-content");
            content.InnerHtml.AppendHtml(AdminTabStripTagHelper.Pane(standardContent, true));
            for (var i = 0; i < LanguagesIds.Count; i++)
                content.InnerHtml.AppendHtml(AdminTabStripTagHelper.Pane(LocalizedTemplate(i), false));
            strip.InnerHtml.AppendHtml(content);

            output.TagName = null;
            output.Content.SetHtmlContent(strip);
        }
        else
        {
            output.TagName = null;
            output.Content.SetHtmlContent(standardContent);
        }
    }
}