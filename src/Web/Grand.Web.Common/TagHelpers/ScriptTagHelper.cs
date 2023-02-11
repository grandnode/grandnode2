﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers
{

    [HtmlTargetElement("script", Attributes = LocationAttributeName)]
    [HtmlTargetElement("script", Attributes = SrcAttributeName)]
    [HtmlTargetElement("script", Attributes = OrderAttributeName)]
    public class ScriptTagHelper : TagHelper
    {

        private const string LocationAttributeName = "asp-location";
        private const string SrcAttributeName = "asp-src";
        private const string OrderAttributeName = "asp-order";

        [HtmlAttributeName(LocationAttributeName)]
        public ScriptLocation Location { get; set; }

        [HtmlAttributeName(SrcAttributeName)]
        public string Src { get; set; }

        [HtmlAttributeName(OrderAttributeName)]
        public int DisplayOrder { get; set; }

        private readonly IResourceManager _resourceManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ScriptTagHelper(IResourceManager resourceManager, IHttpContextAccessor httpContextAccessor)
        {
            _resourceManager = resourceManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var isAjaxCall = _httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Request.Headers["x-requested-with"] == "XMLHttpRequest";
            if (!isAjaxCall)
            {
                output.SuppressOutput();
               
                var childContent = await output.GetChildContentAsync();

                var builder = new TagBuilder("script");
                builder.InnerHtml.AppendHtml(childContent);
                builder.TagRenderMode = TagRenderMode.Normal;
                if(!string.IsNullOrEmpty(Src))
                {
                    builder.Attributes.Add("src", Src);
                }
                foreach (var attribute in output.Attributes)
                {
                    builder.Attributes.Add(attribute.Name, attribute.Value.ToString());
                }

                switch (Location)
                {
                    case ScriptLocation.Head:
                        _resourceManager.RegisterHeadScript(builder, DisplayOrder);
                        break;

                    case ScriptLocation.Header:
                        _resourceManager.RegisterHeaderScript(builder, DisplayOrder);
                        break;

                    case ScriptLocation.Footer:
                        _resourceManager.RegisterFootScript(builder, DisplayOrder);
                        break;

                    default:
                        break;
                }
            }
        }
        
    }
}
