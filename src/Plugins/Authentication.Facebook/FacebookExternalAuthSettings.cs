using Grand.Domain.Configuration;

namespace Authentication.Facebook;

/// <summary>
///     Represents settings of the Facebook authentication method
/// </summary>
public class FacebookExternalAuthSettings : ISettings
{
    /// <summary>
    ///     Gets or sets display order
    /// </summary>
    public int DisplayOrder { get; set; }
}