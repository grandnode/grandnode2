using Grand.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Grand.Web.Common.Middleware;

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
    ///     Invoke middleware actions
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        //whether database is installed
        if (!DataSettingsManager.DatabaseIsInstalled())
        {
            var featureManager = context.RequestServices.GetService<IFeatureManager>();
            var isInstallerModuleEnabled = await featureManager.IsEnabledAsync("Grand.Module.Installer");
            if (!isInstallerModuleEnabled)
            {
                // Return a response indicating the installer module is not enabled
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("The installation module is not enabled.");
                return;
            }

            const string installUrl = "/install";
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