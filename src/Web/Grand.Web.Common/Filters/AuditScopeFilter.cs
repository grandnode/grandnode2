using Grand.Data.Audit;
using Grand.Domain.Audit;
using Grand.Infrastructure;
using Grand.Infrastructure.Audit;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Activates the audit tracker for staff requests (the Admin, Store and Vendor panels, the API and SCIM, and the
///     storefront while a staff member impersonates a customer), and flushes it when the request ends.
///     The repositories record what changed; this filter decides whether to record and who acted.
/// </summary>
public class AuditScopeFilter(IAuditTracker tracker, IContextAccessor contextAccessor) : IAsyncResourceFilter
{
    private const string AreaAdmin = "Admin";
    private const string AreaStore = "Store";
    private const string AreaVendor = "Vendor";

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var actor = ResolveActor(context);
        if (actor == null)
        {
            await next();
            return;
        }

        tracker.Activate(actor);
        try
        {
            await next();
        }
        finally
        {
            await tracker.FlushAsync();
        }
    }

    private AuditActor ResolveActor(ResourceExecutingContext context)
    {
        var httpContext = context.HttpContext;
        var workContext = contextAccessor.WorkContext;
        var original = workContext?.OriginalCustomerIfImpersonated;
        var source = ResolveSource(context, original != null);
        if (source == null)
            return null;

        //SCIM checks its bearer token in its own filter, not through an authentication scheme
        if (source != AuditSource.Scim && httpContext.User.Identity?.IsAuthenticated != true)
            return null;

        var current = workContext?.CurrentCustomer;
        var staff = original ?? current;
        var customerId = staff?.Id;
        var email = staff?.Email;
        if (source == AuditSource.Scim && string.IsNullOrEmpty(email))
        {
            customerId = null;
            email = "SCIM";
        }

        var routeValues = context.RouteData.Values;
        return new AuditActor(
            source.Value,
            customerId,
            email,
            original != null ? current?.Id : null,
            staff?.StaffStoreId,
            staff?.VendorId,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            $"{source.Value} / {routeValues["controller"]} / {routeValues["action"]}",
            Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
    }

    private static AuditSource? ResolveSource(ResourceExecutingContext context, bool impersonating)
    {
        var marked = context.ActionDescriptor.EndpointMetadata?.OfType<AuditSourceAttribute>().LastOrDefault();
        if (marked != null)
            return marked.Source;

        return context.RouteData.Values["area"] as string switch {
            AreaAdmin => AuditSource.Admin,
            AreaStore => AuditSource.Store,
            AreaVendor => AuditSource.Vendor,
            null or "" when impersonating => AuditSource.Storefront,
            _ => null
        };
    }
}
