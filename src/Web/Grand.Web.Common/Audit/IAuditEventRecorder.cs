using Grand.Domain.Customers;

namespace Grand.Web.Common.Audit;

/// <summary>
///     Records the audit events that are not data changes. Never throws: a failure is logged.
/// </summary>
public interface IAuditEventRecorder
{
    Task SignIn(Customer customer);

    Task SignInFailed(Customer customer, string reason);

    Task SignOut(Customer customer);

    /// <summary>
    ///     Records the sign-out of the staff member behind the current request
    /// </summary>
    Task SignOutCurrent();

    /// <summary>
    ///     Records that the current staff member exported entity data as a file
    /// </summary>
    Task Export(string entityType, string filterSummary, int? rowCount);

    Task ImpersonationStart(Customer target);

    Task ImpersonationEnd(Customer target);

    Task AuditLogViewed();

    Task AuditLogExported(string filterSummary, int rowCount);

    /// <summary>
    ///     Records that the current staff member was refused an admin page or action for lack of a permission
    /// </summary>
    Task AccessDenied(string permission, string permissionAction);

    /// <summary>
    ///     Records a plugin installed, uninstalled, removed or uploaded by the current staff member
    /// </summary>
    Task Plugin(string pluginName, string change);

    /// <summary>
    ///     Records, on a record, a change made through a separate record that belongs to it (a note of an order), so it
    ///     shows in the owner's change history
    /// </summary>
    Task OwnedChange(string entityType, string entityId, string entityName, string path, string oldValue,
        string newValue);
}
