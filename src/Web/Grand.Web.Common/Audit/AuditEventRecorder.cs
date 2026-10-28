using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Audit;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Grand.Web.Common.Audit;

public class AuditEventRecorder(
    IAuditService auditService,
    AuditSettings auditSettings,
    IGroupService groupService,
    IHttpContextAccessor httpContextAccessor,
    IContextAccessor contextAccessor,
    ILogger<AuditEventRecorder> logger) : IAuditEventRecorder
{
    public Task SignIn(Customer customer)
    {
        return RecordSignInEvent(AuditKind.SignIn, customer, null);
    }

    public Task SignInFailed(Customer customer, string reason)
    {
        return RecordSignInEvent(AuditKind.SignInFailed, customer, reason);
    }

    public Task SignOut(Customer customer)
    {
        return RecordSignInEvent(AuditKind.SignOut, customer, null);
    }

    public Task SignOutCurrent()
    {
        return RecordSignInEvent(AuditKind.SignOut, CurrentStaff(), null);
    }

    public Task Export(string entityType, string filterSummary, int? rowCount)
    {
        var details = string.IsNullOrEmpty(filterSummary) ? "filter: none" : $"filter: {filterSummary}";
        if (rowCount.HasValue)
            details += $"; rows: {rowCount}";
        return Record(AuditKind.Export, CurrentStaff(), entry =>
        {
            entry.EntityType = entityType;
            entry.Details = details;
        });
    }

    public Task ImpersonationStart(Customer target)
    {
        return Record(AuditKind.ImpersonationStart, CurrentStaff(), entry => Target(entry, target));
    }

    public Task ImpersonationEnd(Customer target)
    {
        return Record(AuditKind.ImpersonationEnd, CurrentStaff(), entry => Target(entry, target));
    }

    public Task AuditLogViewed()
    {
        return Record(AuditKind.AuditLogViewed, CurrentStaff(), null);
    }

    public Task AuditLogExported(string filterSummary, int rowCount)
    {
        return Record(AuditKind.AuditLogExported, CurrentStaff(),
            entry => entry.Details = $"filter: {filterSummary}; rows: {rowCount}");
    }

    private async Task RecordSignInEvent(AuditKind kind, Customer customer, string reason)
    {
        try
        {
            if (!auditSettings.LogStaffSignIns || customer == null || !await IsStaff(customer))
                return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit {Kind} of customer {CustomerId} was not recorded", kind, customer?.Id);
            return;
        }

        await Record(kind, customer, entry =>
        {
            entry.EntityType = nameof(Customer);
            entry.EntityId = customer.Id;
            entry.EntityName = customer.Email;
            entry.Details = reason;
        });
    }

    private async Task<bool> IsStaff(Customer customer)
    {
        return await groupService.IsAdmin(customer)
               || await groupService.IsStoreManager(customer)
               || await groupService.IsVendor(customer)
               || await groupService.IsSalesManager(customer);
    }

    //the staff member behind the request, also while impersonating a customer
    private Customer CurrentStaff()
    {
        var workContext = contextAccessor.WorkContext;
        return workContext?.OriginalCustomerIfImpersonated ?? workContext?.CurrentCustomer;
    }

    private static void Target(AuditEntry entry, Customer target)
    {
        entry.EntityType = nameof(Customer);
        entry.EntityId = target?.Id;
        entry.EntityName = target?.Email;
        entry.ImpersonatedCustomerId = target?.Id;
    }

    private async Task Record(AuditKind kind, Customer actor, Action<AuditEntry> fill)
    {
        try
        {
            var entry = new AuditEntry {
                Kind = kind,
                ActorCustomerId = actor?.Id,
                ActorEmail = actor?.Email,
                StoreId = actor?.StaffStoreId,
                VendorId = actor?.VendorId
            };
            FillRequest(entry);
            fill?.Invoke(entry);
            await auditService.InsertEvent(entry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit {Kind} of customer {CustomerId} was not recorded", kind, actor?.Id);
        }
    }

    private void FillRequest(AuditEntry entry)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        var routeValues = httpContext.Request.RouteValues;
        entry.Source = (routeValues["area"] as string) switch {
            "Admin" => AuditSource.Admin,
            "Store" => AuditSource.Store,
            "Vendor" => AuditSource.Vendor,
            _ => AuditSource.Storefront
        };
        entry.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        entry.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
        entry.Action = $"{entry.Source} / {routeValues["controller"]} / {routeValues["action"]}";
        entry.OperationId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
    }
}
