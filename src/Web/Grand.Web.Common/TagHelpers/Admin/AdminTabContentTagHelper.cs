﻿using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin
{
    [HtmlTargetElement("content", ParentTag = "tabstrip-item")]
    public class AdminTabContentTagHelper : TagHelper
    {
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();
            var list = (List<string>)ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName];
            list.Add(content.GetContent());
            output.SuppressOutput();
        }

    }
}
