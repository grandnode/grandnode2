namespace Grand.Infrastructure.Configuration;

public class AccessControlConfig
{
    /// <summary>
    ///     A value indicating whether to ignore ACL rules (side-wide). It can significantly improve performance when enabled.
    /// </summary>
    public bool IgnoreAcl { get; set; }

    /// <summary>
    ///     A value indicating whether to ignore "limit per store" rules (side-wide). It can significantly improve performance
    ///     when enabled.
    /// </summary>
    public bool IgnoreStoreLimitations { get; set; }
}