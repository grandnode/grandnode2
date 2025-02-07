#nullable enable

using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    ///     Invoke middleware actions
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var databaseIsInstalled = DataSettingsManager.DatabaseIsInstalled();
        var version = await GetDatabaseVersion(context, databaseIsInstalled);

        if (version == null)
        {
            if (!IsInstallUrl(context.Request))
            {
                //redirect
                context.Response.Redirect(InstallUrl);
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
            if (IsInstallUrl(context.Request))
            {
                //redirect
                context.Response.Redirect("/");
                return;
            }
        }

        //call the next middleware in the request pipeline
        await _next(context);
    }
    private Task<GrandNodeVersion?> GetDatabaseVersion(HttpContext context, bool databaseIsInstalled)
    {
        return _cacheBase.GetAsync(CacheKey.GRAND_NODE_VERSION, () =>
        {
            if (databaseIsInstalled) 
                return Task.FromResult(context.RequestServices.GetRequiredService<IRepository<GrandNodeVersion>>().Table.FirstOrDefault());

            return Task.FromResult<GrandNodeVersion?>(null);

        }, int.MaxValue);
    }

    private bool IsInstallUrl(HttpRequest request)
    {
        return request.GetEncodedPathAndQuery().StartsWith(InstallUrl, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
