﻿using Grand.Web.Common.Events;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wangkanai.Detection.Models;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common.TagHelpers.Admin
{
    [HtmlTargetElement("admin-tabstrip")]
    public partial class AdminTabStripTagHelper : TagHelper
    {
        private readonly IMediator _mediator;
        private readonly IDetectionService _detectionService;

        public AdminTabStripTagHelper(IMediator mediator, IDetectionService detectionService)
        {
            _mediator = mediator;
            _detectionService = detectionService;
        }

        [HtmlAttributeName("SetTabPos")]
        public bool SetTabPos { get; set; } = false;

        [HtmlAttributeName("BindGrid")]
        public bool BindGrid { get; set; } = false;

        [HtmlAttributeName("Name")]
        public string Name { get; set; }

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName] = new List<string>();
            var _ = await output.GetChildContentAsync();
            var list = (List<string>)ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName];
            if (_detectionService.Device.Type == Device.Mobile || _detectionService.Device.Type == Device.Tablet)
                SetTabPos = false;

            var selectedTabIndex = GetSelectedTabIndex();

            output.TagName = "div";
            output.Attributes.SetAttribute("id", Name);
            output.Attributes.SetAttribute("style", "display:none");
            var rnd = new Random().Next(0, 100);
            var sb = new StringBuilder();
            sb.AppendLine("<script>");
            sb.AppendLine("$(document).ready(function () {");
            sb.AppendLine($"$('#{Name}').show();");
            sb.AppendLine($"var tab_{rnd} = $('#{Name}').kendoTabStrip({{ ");
            sb.AppendLine($"    tabPosition: '{(SetTabPos ? "left" : "top")}',");
            sb.AppendLine($"    animation: {{ open: {{ effects: 'fadeIn'}} }},");
            sb.AppendLine("     select: tabstrip_on_tab_select,");
            if (BindGrid)
                sb.AppendLine("     show: tabstrip_on_tab_show");
            sb.AppendLine("  }).data('kendoTabStrip');");

            var eventMessage = new AdminTabStripCreated(Name);
            await _mediator.Publish(eventMessage);
            int i = 0;
            foreach (var eventBlock in eventMessage.BlocksToRender)
            {
                i++;
                sb.AppendLine($"tab_{rnd}.append({{");
                sb.AppendLine($"    text: '{eventBlock.tabname}',");
                sb.AppendLine($"    content: '{eventBlock.content}'");
                sb.AppendLine("});");
            }


            if (BindGrid && selectedTabIndex > 0)
            {
                sb.AppendLine("$(window).load(function() {");
                sb.AppendLine($"  var selectedtab_{rnd} = $('#{Name}').data('kendoTabStrip').select(); ");
                sb.AppendLine($"  tabstrip_on_tab_show(selectedtab_{rnd}, true); ");
                sb.AppendLine("});");
            }

            sb.AppendLine("})");

            

            sb.AppendLine("</script>");
            sb.AppendLine($"<input type='hidden' id='selected-tab-index' name='selected-tab-index' value='{selectedTabIndex}'>");


            output.PostContent.AppendHtml(string.Concat(list));
            output.PreElement.AppendHtml(sb.ToString());
        }

        private int GetSelectedTabIndex()
        {
            int index = 0;
            string dataKey = "Grand.selected-tab-index";
            if (ViewContext.ViewData[dataKey] is int)
            {
                index = (int)ViewContext.ViewData[dataKey];
            }
            if (ViewContext.TempData[dataKey] is int)
            {
                index = (int)ViewContext.TempData[dataKey];
            }

            //ensure it's not negative
            if (index < 0)
                index = 0;

            return index;
        }

    }
}
