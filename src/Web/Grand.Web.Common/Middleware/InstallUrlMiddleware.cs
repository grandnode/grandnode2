using Grand.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Threading.Tasks;

namespace Grand.Web.Common.Middleware
{
    public class InstallUrlMiddleware
    {
        #region Fields

        private readonly RequestDelegate _next;

        #endregion

        #region Ctor

        public InstallUrlMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke middleware actions
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="webHelper">Web helper</param>
        /// <returns>Task</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            //whether database is installed
            if (!DataSettingsManager.DatabaseIsInstalled())
            {
                var installUrl = $"/install";
                if (!context.Request.GetEncodedPathAndQuery().StartsWith(installUrl, StringComparison.OrdinalIgnoreCase))
                {
                    //redirect
                    context.Response.Redirect(installUrl);
                    return;
                }
            }

            //or call the next middleware in the request pipeline
            await _next(context);
        }

        #endregion
    }
}
