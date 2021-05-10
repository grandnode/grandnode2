using Grand.Web.Common.Page;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace Grand.Web.Common.TagHelpers
{
    [HtmlTargetElement("meta-description", TagStructure = TagStructure.WithoutEndTag)]
    public class MetaDescriptionTagHelper : TagHelper
    {

        private readonly IPageHeadBuilder _pageHeadBuilder;

        public MetaDescriptionTagHelper(IPageHeadBuilder pageHeadBuilder)
        {
            _pageHeadBuilder = pageHeadBuilder;
        }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "meta";
            output.Attributes.Add("name", "description");
            output.Attributes.Add("content", _pageHeadBuilder.GenerateMetaDescription());
            return Task.CompletedTask;
        }

    }
}
