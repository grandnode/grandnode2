using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Grand.Business.Core.Extensions;
using Grand.SharedKernel.Extensions;
using Grand.Domain.Tasks;
using Scryber;
using DotLiquid.Util;

namespace Grand.Web.Common.Middleware;

public class ContextMiddleware
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
    public ContextMiddleware(RequestDelegate next)
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
    public async Task InvokeAsync(HttpContext context, IContextAccessor contextAccessor)
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

        var storeContext = context.RequestServices.GetRequiredService<IStoreContextSetter>();
        contextAccessor.StoreContext = await storeContext.InitializeStoreContext();

        var workContextSetter = context.RequestServices.GetRequiredService<IWorkContextSetter>();
        contextAccessor.WorkContext = await workContextSetter.InitializeWorkContext(contextAccessor.StoreContext.CurrentStore.Id);

        //call the next middleware in the request pipeline
        await _next(context);
    }

    #endregion
}