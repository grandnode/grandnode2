﻿using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters
{
    public class ArgumentNameFilterAttribute : ActionFilterAttribute
    {
        public string KeyName { get; set; }
        public string Argument { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context?.HttpContext.Request == null)
                return;

            context.ActionArguments[Argument] = context.HttpContext.Request.Form.Keys.Any(key => key.Equals(KeyName));

        }
    }
}
