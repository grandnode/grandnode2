namespace Grand.Infrastructure.Configuration;

/// <summary>
///     Represents the audit trail config (bound from the "Audit" section of appsettings.json). It is set by whoever
///     runs the installation, not in the admin panel, so a staff member cannot shorten or switch off what the trail
///     keeps.
/// </summary>
public class AuditConfig
{
    /// <summary>
    ///     Days an entry is kept before the daily retention task deletes it; 0 or less keeps entries forever
    /// </summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    ///     Records staff sign-ins, failed sign-ins and sign-outs
    /// </summary>
    public bool LogStaffSignIns { get; set; } = true;
}
