﻿using Grand.Web.Common.Page;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers
{
    [HtmlTargetElement("head-custom", TagStructure = TagStructure.WithoutEndTag)]
    public class HeadCustomTagHelper : TagHelper
    {

        private readonly IPageHeadBuilder _pageHeadBuilder;

        public HeadCustomTagHelper(IPageHeadBuilder pageHeadBuilder)
        {
            _pageHeadBuilder = pageHeadBuilder;
        }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();
            output.Content.SetHtmlContent(_pageHeadBuilder.GenerateHeadCustom());
            return Task.CompletedTask;
        }
    }
}