using Grand.Data.Audit;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;

namespace Grand.Web.Common.Audit;

/// <summary>
///     Describes a request the same way for the data-change scope and for the recorded events, so the entries of
///     one operation agree on source and action
/// </summary>
public static class AuditRequest
{
    /// <summary>
    ///     The source of a panel, API or SCIM request: an [AuditSource] mark wins over the area. Null otherwise.
    /// </summary>
    public static AuditSource? StaffSource(IEnumerable<object> endpointMetadata, RouteValueDictionary routeValues)
    {
        var marked = endpointMetadata?.OfType<AuditSourceAttribute>().LastOrDefault();
        if (marked != null)
            return marked.Source;

        return routeValues?["area"] as string switch {
            "Admin" => AuditSource.Admin,
            "Store" => AuditSource.Store,
            "Vendor" => AuditSource.Vendor,
            _ => null
        };
    }

    public static AuditActor Actor(HttpContext httpContext, RouteValueDictionary routeValues, AuditSource source,
        string customerId, string email, string impersonatedCustomerId, string storeId, string vendorId)
    {
        return new AuditActor(
            source,
            customerId,
            email,
            impersonatedCustomerId,
            storeId,
            vendorId,
            httpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContext?.Request.Headers.UserAgent.ToString(),
            httpContext == null ? null : $"{source} / {routeValues["controller"]} / {routeValues["action"]}",
            httpContext == null ? null : Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
    }
}
