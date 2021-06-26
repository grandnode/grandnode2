using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers
{

    [HtmlTargetElement("style", Attributes = LocationAttributeName)]
    public class StyleTagHelper : TagHelper
    {

        private const string LocationAttributeName = "asp-location";

        [HtmlAttributeName(LocationAttributeName)]
        public StyleLocation Location { get; set; }

        private readonly IResourceManager _resourceManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StyleTagHelper(IResourceManager resourceManager, IHttpContextAccessor httpContextAccessor)
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

                var builder = new TagBuilder("style");
                builder.InnerHtml.AppendHtml(childContent);
                builder.TagRenderMode = TagRenderMode.Normal;

                foreach (var attribute in output.Attributes)
                {
                    builder.Attributes.Add(attribute.Name, attribute.Value.ToString());
                }

                switch (Location)
                {
                    case StyleLocation.Head:
                        _resourceManager.RegisterHeadScript(builder);
                        break;

                    case StyleLocation.Header:
                        _resourceManager.RegisterHeaderScript(builder);
                        break;

                    case StyleLocation.Footer:
                        _resourceManager.RegisterFootScript(builder);
                        break;

                    default:
                        break;
                }
            }
        }

    }
}