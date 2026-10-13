using Grand.Business.Core.Utilities.Authentication;

namespace Authentication.OpenIdConnect.Models;

public class FailedModel
{
    public OidcLoginRefusal Reason { get; set; }
    public bool Mismatch { get; set; }

    /// <summary>The provider identity is already connected to another account</summary>
    public bool IdentityTaken { get; set; }

    /// <summary>Another account is signed in to the store</summary>
    public bool SignedInAsOtherAccount { get; set; }

    /// <summary>A back-office sign-in without a refresh token from the provider: the session could not be checked later</summary>
    public bool SessionUnavailable { get; set; }
}
