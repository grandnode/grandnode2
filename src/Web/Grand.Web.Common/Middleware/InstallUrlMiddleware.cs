#nullable enable

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
    private const string InstallUrl = "/install";

    #endregion

    #region Ctor

    public InstallUrlMiddleware(RequestDelegate next, ICacheBase cacheBase)
    {
        _next = next;
        _cacheBase = cacheBase;
    }

    #endregion

    #region Methods

    public async Task InvokeAsync(HttpContext context)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
        {
            await HandleInstallationAsync(context);
            return;
        }

        var version = GetDatabaseVersion(context);

        if (version == null)
        {
            await HandleInstallationAsync(context);
            return;
        }

        if (!version.DataBaseVersion.Equals(GrandVersion.SupportedDBVersion))
        {
            await context.Response.WriteAsync($"The database version is not supported in this software version. Supported version: {GrandVersion.SupportedDBVersion}, your version: {version.DataBaseVersion}");
            return;
        }

        if (IsInstallUrl(context.Request))
        {
            context.Response.Redirect("/");
            return;
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }

    private async Task HandleInstallationAsync(HttpContext context)
    {
        var featureManager = context.RequestServices.GetRequiredService<IFeatureManager>();
        var isInstallerModuleEnabled = await featureManager.IsEnabledAsync("Grand.Module.Installer");

        if (!isInstallerModuleEnabled)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("The installation module is not enabled.");
            return;
        }

        if (!IsInstallUrl(context.Request))
        {
            context.Response.Redirect(InstallUrl);
        }
    }

    private GrandNodeVersion? GetDatabaseVersion(HttpContext context)
    {
        return _cacheBase.Get(CacheKey.GRAND_NODE_VERSION, () =>
        {
            var repository = context.RequestServices.GetRequiredService<IRepository<GrandNodeVersion>>();
            return repository.Table.FirstOrDefault();
        }, int.MaxValue);
    }

    private bool IsInstallUrl(HttpRequest request)
    {
        return request.GetEncodedPathAndQuery().StartsWith(InstallUrl, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
