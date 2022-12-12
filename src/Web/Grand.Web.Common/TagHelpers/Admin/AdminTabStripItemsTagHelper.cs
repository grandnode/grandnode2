﻿using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin
{
    [HtmlTargetElement("items", ParentTag = "admin-tabstrip")]
    public class AdminTabStripItemsTagHelper : TagHelper
    {
        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "ul";
            return Task.CompletedTask;
        }
    }
}
