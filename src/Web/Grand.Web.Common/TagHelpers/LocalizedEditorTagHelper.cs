using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Web.Common.TagHelpers
{
    public class LocalizedEditorTagHelper : TagHelper
    {
        [HtmlAttributeName("name")]
        public string Name { get; set; }
        [HtmlAttributeName("ignore-if-severa-stores")]
        public bool IgnoreIfSeveralStores { get; set; } = false;
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }
        [HtmlAttributeName("localized-template")]
        public Func<int, HelperResult> LocalizedTemplate { get; set; }
        [HtmlAttributeName("language-ids")]
        public List<string> LanguagesIds { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var localizationSupported = LanguagesIds.Count > 1;
            var standardContent = (await output.GetChildContentAsync()).GetContent();
            if (IgnoreIfSeveralStores)
            {
                var storeService = ViewContext.HttpContext.RequestServices.GetRequiredService<IStoreService>();
                if ((await storeService.GetAllStores()).Count >= 2)
                {
                    localizationSupported = false;
                }
            }

            if (localizationSupported)
            {
                var tabStrip = new StringBuilder();
                tabStrip.AppendLine(string.Format("<div id='{0}'>", Name));
                tabStrip.AppendLine("<ul>");

                //default tab
                tabStrip.AppendLine("<li class='k-state-active'>");
                tabStrip.AppendLine("Standard");
                tabStrip.AppendLine("</li>");

                var languageService = ViewContext.HttpContext.RequestServices.GetRequiredService<ILanguageService>();

                foreach (var locale in LanguagesIds)
                {
                    //languages
                    var language = await languageService.GetLanguageById(locale);

                    tabStrip.AppendLine("<li>");
                    var urlHelper = new UrlHelper(ViewContext);
                    var iconUrl = urlHelper.Content("~/assets/images/flags/" + language.FlagImageFileName);
                    tabStrip.AppendLine(string.Format("<img class='k-image' alt='' src='{0}'>", iconUrl));
                    tabStrip.AppendLine(WebUtility.HtmlEncode(language.Name));
                    tabStrip.AppendLine("</li>");
                }

                tabStrip.AppendLine("</ul>");

                //default tab
                tabStrip.AppendLine("<div>");
                tabStrip.AppendLine(standardContent);
                tabStrip.AppendLine("</div>");

                for (int i = 0; i < LanguagesIds.Count; i++)
                {
                    //languages
                    tabStrip.AppendLine("<div>");
                    tabStrip.AppendLine(LocalizedTemplate(i).ToHtmlString());
                    tabStrip.AppendLine("</div>");
                }

                tabStrip.AppendLine("</div>");
                tabStrip.AppendLine("<script>");
                tabStrip.AppendLine("$(document).ready(function() {");
                tabStrip.AppendLine(string.Format("$('#{0}').kendoTabStrip(", Name));
                tabStrip.AppendLine("{");
                tabStrip.AppendLine("animation:  {");
                tabStrip.AppendLine("open: {");
                tabStrip.AppendLine("effects: \"fadeIn\"");
                tabStrip.AppendLine("}");
                tabStrip.AppendLine("}");
                tabStrip.AppendLine("});");
                tabStrip.AppendLine("});");
                tabStrip.AppendLine("</script>");
                output.TagName = null;
                output.Content.SetHtmlContent(tabStrip.ToString());
            }
            else
            {
                output.TagName = null;
                output.Content.SetHtmlContent(standardContent);
            }
        }
    }
}
