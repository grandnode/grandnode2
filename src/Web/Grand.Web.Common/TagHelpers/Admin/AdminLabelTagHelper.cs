using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace Grand.Web.Common.TagHelpers.Admin
{

    [HtmlTargetElement("admin-label", Attributes = ForAttributeName)]
    public class LabelRequiredTagHelper : LabelTagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string DisplayHintAttributeName = "asp-display-hint";

        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;

        public LabelRequiredTagHelper(IHtmlGenerator generator, IWorkContext workContext, ITranslationService translationService) : base(generator)
        {
            _workContext = workContext;
            _translationService = translationService;
        }

        [HtmlAttributeName(DisplayHintAttributeName)]
        public bool DisplayHint { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);
            output.TagName = "label";
            output.TagMode = TagMode.StartTagAndEndTag;
            var classValue = output.Attributes.ContainsName("class")
                                ? $"{output.Attributes["class"].Value}"
                                : "control-label col-md-3 col-sm-3";
            output.Attributes.SetAttribute("class", classValue);

            var resourceDisplayName = For.Metadata.GetDisplayName();

            var langId = _workContext.WorkingLanguage.Id;

            var resource = _translationService.GetResource(
                resourceDisplayName.ToLowerInvariant(), langId, returnEmptyIfNotFound: true);

            if (!string.IsNullOrEmpty(resource))
            {
                output.Content.SetContent(resource);
            }

            if (resourceDisplayName != null && DisplayHint)
            {

                var hintResource = _translationService.GetResource(
                    resourceDisplayName + ".Hint", langId, returnEmptyIfNotFound: true);

                if (!string.IsNullOrEmpty(hintResource))
                {
                    output.Content.AppendHtml($"<p class='hint'>{hintResource}</p>");
                }
            }
        }
    }
}