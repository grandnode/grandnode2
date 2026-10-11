namespace Grand.Business.Core.Utilities.Authentication;

public static class OidcSessionDefaults
{
    /// <summary>Claim of the main authentication cookie holding the OidcSession id</summary>
    public const string SessionIdClaimType = "grand:oidcSession";

    /// <summary>Data Protection purpose of stored refresh tokens</summary>
    public const string RefreshTokenPurpose = "Grand.OidcSession.RefreshToken";

    /// <summary>How long an ended or expired session record is kept before the cleanup task deletes it</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(7);
}
