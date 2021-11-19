﻿using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers
{
    [HtmlTargetElement("resources", Attributes = AttributeType)]
    public class ResourcesTagHelper : TagHelper
    {
        private const string AttributeType = "asp-type";

        [HtmlAttributeName(AttributeType)]
        public ResourceType Type { get; set; }

        private readonly IResourceManager _resourceManager;

        public ResourcesTagHelper(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public override void Process(TagHelperContext tagHelperContext, TagHelperOutput output)
        {
            switch (Type)
            {
                case ResourceType.ScriptHeader:
                    _resourceManager.RenderHeaderScript(output.Content);
                    break;

                case ResourceType.ScriptFooter:
                    _resourceManager.RenderFootScript(output.Content);
                    break;

                case ResourceType.HeadLink:
                    _resourceManager.RenderHeadLink(output.Content);
                    break;

                case ResourceType.HeadScript:
                    _resourceManager.RenderHeadScript(output.Content);
                    break;

                case ResourceType.TemplateHeader:
                    _resourceManager.RenderTemplate(output.Content, true);
                    break;

                case ResourceType.TemplateFooter:
                    _resourceManager.RenderTemplate(output.Content, false);
                    break;

                default:
                    break;
            }

            output.TagName = null;
        }
    }
}
