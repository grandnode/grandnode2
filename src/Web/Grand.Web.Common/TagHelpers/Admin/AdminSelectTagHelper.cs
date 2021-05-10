using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace Grand.Web.Common.TagHelpers.Admin
{

    [HtmlTargetElement("admin-select", Attributes = ForAttributeName)]
    public class AdminSelectTagHelper : Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string DisplayHintAttributeName = "asp-display-hint";

        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;

        public AdminSelectTagHelper(IHtmlGenerator generator, IWorkContext workContext, ITranslationService translationService) : base(generator)
        {
            _workContext = workContext;
            _translationService = translationService;
        }

        [HtmlAttributeName(DisplayHintAttributeName)]
        public bool DisplayHint { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);
            output.TagName = "select";
            output.TagMode = TagMode.StartTagAndEndTag;
            var classValue = "form-control k-input ";
            TagHelperAttribute forAttribute;
            if (context.AllAttributes.TryGetAttribute("class", out forAttribute))
            {
                classValue += forAttribute.Value.ToString();
            }
            output.Attributes.SetAttribute("class", classValue);
        }
    }


}