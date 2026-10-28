namespace Grand.Domain.Audit;

/// <summary>
///     What an audit entry records
/// </summary>
public enum AuditKind
{
    Insert = 0,
    Update = 1,
    Delete = 2,
    Bulk = 3,
    SignIn = 10,
    SignInFailed = 11,
    SignOut = 12,
    Export = 20,
    ImpersonationStart = 30,
    ImpersonationEnd = 31,
    AuditLogViewed = 40,
    AuditLogExported = 41
}
