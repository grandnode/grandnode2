using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers
{

    [HtmlTargetElement("template", Attributes = AttributeNameHeader)]
    [HtmlTargetElement("template", Attributes = AttributeNameFooter)]
    public class TemplateTagHelper : TagHelper
    {
        private const string AttributeNameHeader = "asp-header";

        private const string AttributeNameFooter = "asp-footer";

        [HtmlAttributeName(AttributeNameHeader)]
        public bool Header { get; set; }

        [HtmlAttributeName(AttributeNameFooter)]
        public bool Footer { get; set; }

        private readonly IResourceManager _resourceManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TemplateTagHelper(IResourceManager resourceManager, IHttpContextAccessor httpContextAccessor)
        {
            _resourceManager = resourceManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            bool isAjaxCall = _httpContextAccessor.HttpContext.Request.Headers["x-requested-with"] == "XMLHttpRequest";
            if (!isAjaxCall)
            {
                output.SuppressOutput();
               
                var childContent = await output.GetChildContentAsync();

                var builder = new TagBuilder("template");
                builder.InnerHtml.AppendHtml(childContent);
                builder.TagRenderMode = TagRenderMode.Normal;                
                foreach (var attribute in output.Attributes)
                {
                    builder.Attributes.Add(attribute.Name, attribute.Value.ToString());
                }

                if (Header && !Footer)
                {
                    _resourceManager.RegisterTemplate(builder, true);
                }
                if (!Header && Footer)
                {
                    _resourceManager.RegisterTemplate(builder, false);
                }

            }
        }
        
    }
}
