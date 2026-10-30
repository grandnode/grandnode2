using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Audit;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Audit;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.Audit;

public class AuditEventRecorder(
    IAuditService auditService,
    AuditConfig auditConfig,
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

    public Task AccessDenied(string permission, string permissionAction)
    {
        var details = string.IsNullOrEmpty(permissionAction)
            ? $"permission: {permission}"
            : $"permission: {permission} / {permissionAction}";
        return Record(AuditKind.AccessDenied, CurrentStaff(), entry => entry.Details = details);
    }

    public Task Plugin(string pluginName, string change)
    {
        return Record(AuditKind.Plugin, CurrentStaff(), entry =>
        {
            entry.EntityType = "Plugin";
            entry.EntityName = pluginName;
            entry.Details = change;
        });
    }

    public Task OwnedChange(string entityType, string entityId, string entityName, string entityStoreId, string path,
        string oldValue, string newValue)
    {
        return Record(AuditKind.Update, CurrentStaff(), entry =>
        {
            entry.EntityType = entityType;
            entry.EntityId = entityId;
            entry.EntityName = entityName;
            entry.EntityStoreId = entityStoreId;
            entry.Changes = [AuditDiff.Change(path, oldValue, newValue)];
        });
    }

    private async Task RecordSignInEvent(AuditKind kind, Customer customer, string reason)
    {
        try
        {
            if (!auditConfig.LogStaffSignIns || customer == null || !await IsStaff(customer))
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
            //an event outside the panels (a storefront sign-in) is a storefront event
            var httpContext = httpContextAccessor.HttpContext;
            var routeValues = httpContext?.Request.RouteValues;
            var source = AuditRequest.StaffSource(httpContext?.GetEndpoint()?.Metadata, routeValues)
                         ?? AuditSource.Storefront;
            var entry = new AuditEntry { Kind = kind };
            AuditRequest.Actor(httpContext, routeValues, source, actor?.Id, actor?.Email, null, actor?.StaffStoreId,
                actor?.VendorId).Stamp(entry);
            fill?.Invoke(entry);
            await auditService.InsertEvent(entry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit {Kind} of customer {CustomerId} was not recorded", kind, actor?.Id);
        }
    }
}
