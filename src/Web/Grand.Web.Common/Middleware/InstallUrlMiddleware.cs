using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Grand.Web.Common.Middleware;

public class InstallUrlMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Ctor

    public InstallUrlMiddleware(RequestDelegate next, ICacheBase cacheBase)
    {
        _next = next;
        _cacheBase = cacheBase;
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
        var databaseIsInstalled = DataSettingsManager.DatabaseIsInstalled();
        const string installUrl = "/install";
        //whether database is installed
        var version = _cacheBase.Get(CacheKey.GRAND_NODE_VERSION, () =>
        {
            if (databaseIsInstalled)
                return context.RequestServices.GetRequiredService<IRepository<GrandNodeVersion>>().Table.FirstOrDefault();

            return null;
        }, int.MaxValue);

        if (version == null)
        {
            var featureManager = context.RequestServices.GetRequiredService<IFeatureManager>();
            var isInstallerModuleEnabled = await featureManager.IsEnabledAsync("Grand.Module.Installer");
            if (!isInstallerModuleEnabled)
            {
                // Return a response indicating the installer module is not enabled
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("The installation module is not enabled.");
                return;
            }

            if (!context.Request.GetEncodedPathAndQuery().StartsWith(installUrl, StringComparison.OrdinalIgnoreCase))
            {
                //redirect
                context.Response.Redirect(installUrl);
                return;
            }
        }
        else
        {
            if (!version.DataBaseVersion.Equals(GrandVersion.SupportedDBVersion))
            {
                await context.Response.WriteAsync("The database version is not supported in this software version. " +
                                                  $"Supported version: {GrandVersion.SupportedDBVersion} , your version: {version.DataBaseVersion}");
                return;
            }
            if (context.Request.GetEncodedPathAndQuery().StartsWith(installUrl, StringComparison.OrdinalIgnoreCase))
            {
                //redirect
                context.Response.Redirect("/");
                return;
            }
        }

        //call the next middleware in the request pipeline
        await _next(context);
    }

    #endregion
}
