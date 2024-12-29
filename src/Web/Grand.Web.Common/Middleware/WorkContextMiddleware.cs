using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Middleware;

public class WorkContextMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

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
    public async Task InvokeAsync(HttpContext context, IWorkContextSetter workContextSetter, IWorkContextAccessor workContextAccessor)
    {
        if (context?.Request == null) return;

        //set current context
        var workContext = await workContextSetter.InitializeWorkContext();
        workContextAccessor.WorkContext = workContext;

        //call the next middleware in the request pipeline
        await _next(context);
    }

    #endregion
}