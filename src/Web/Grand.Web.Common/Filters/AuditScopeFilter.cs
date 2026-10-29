using Grand.Data.Audit;
using Grand.Domain.Audit;
using Grand.Infrastructure;
using Grand.Web.Common.Audit;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Activates the audit tracker for staff requests (the Admin, Store and Vendor panels, the API and SCIM, and the
///     storefront while a staff member impersonates a customer), and flushes it when the request ends.
///     The repositories record what changed; this filter decides whether to record and who acted.
/// </summary>
public class AuditScopeFilter(IAuditTracker tracker, IContextAccessor contextAccessor) : IAsyncResourceFilter
{
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
            await tracker.CompleteAsync();
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

        return AuditRequest.Actor(httpContext, context.RouteData.Values, source.Value, customerId, email,
            original != null ? current?.Id : null, staff?.StaffStoreId, staff?.VendorId);
    }

    private static AuditSource? ResolveSource(ResourceExecutingContext context, bool impersonating)
    {
        var routeValues = context.RouteData.Values;
        return AuditRequest.StaffSource(context.ActionDescriptor.EndpointMetadata, routeValues)
               ?? (impersonating && string.IsNullOrEmpty(routeValues["area"] as string)
                   ? AuditSource.Storefront
                   : null);
    }
}
