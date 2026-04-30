using Grand.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.Infrastructure;

/// <summary>
///     Handles unhandled exceptions according to ASP.NET Core best practices.
///     For API requests (Bearer token) it writes an RFC 7807 ProblemDetails JSON response.
///     For regular web requests it only logs the error and returns false so the configured
///     error page (or developer exception page) can handle the response.
/// </summary>
public class GrandExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GrandExceptionHandler> _logger;

    public GrandExceptionHandler(ILogger<GrandExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        // Log the exception when the database is available
        if (DataSettingsManager.DatabaseIsInstalled())
            _logger.LogError(exception, "An unhandled exception has occurred");

        // Only write a JSON response for API (Bearer) requests; let the configured
        // exception page handle HTML responses so the developer page / error page works.
        if (!ApplicationBuilderExtensions.IsApiRequest(httpContext.Request))
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetailsService = httpContext.RequestServices.GetService<IProblemDetailsService>();
        if (problemDetailsService != null)
        {
            await problemDetailsService.WriteAsync(new ProblemDetailsContext {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request",
                    Instance = httpContext.Request.Path
                }
            });
        }
        else
        {
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request",
                Instance = httpContext.Request.Path
            }, cancellationToken);
        }

        return true;
    }
}
