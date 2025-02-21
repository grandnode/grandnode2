using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Grand.Business.Core.Extensions;
using Grand.SharedKernel.Extensions;

namespace Grand.Web.Common.Middleware;

public class WorkContextMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

    private readonly List<string> skipRoutePattern = ["/scalar/{documentName}", "/openapi/{documentName}.json", "install"];

    #endregion

    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="next">Next</param>
    public WorkContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Invoke middleware actions
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="workContext">workContext</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context, IWorkContextAccessor workContextAccessor)
    {
        if (context?.Request == null) return;
        
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var routePattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
            if (routePattern != null && skipRoutePattern.Any(pattern => routePattern.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }
        }

        // Set current context
        var storeService = context.RequestServices.GetRequiredService<IStoreService>();
        var currentStore = await storeService.GetStoreByHostOrStoreId(context.Request.Host.Host, context.GetStoreCookie());
        context.Items[CommonHelper.StoreIdItemContext] = currentStore.Id;

        var workContextSetter = context.RequestServices.GetRequiredService<IWorkContextSetter>();
        workContextAccessor.WorkContext = await workContextSetter.InitializeWorkContext(currentStore.Id);

        //call the next middleware in the request pipeline
        await _next(context);
    }

    #endregion
}