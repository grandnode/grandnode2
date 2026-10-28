using Grand.Domain.Configuration;

namespace Grand.Domain.Audit;

public class AuditSettings : ISettings
{
    /// <summary>
    ///     Gets or sets the number of days audit entries are kept
    /// </summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    ///     Gets or sets a value indicating whether staff sign-ins are recorded
    /// </summary>
    public bool LogStaffSignIns { get; set; } = true;
}
