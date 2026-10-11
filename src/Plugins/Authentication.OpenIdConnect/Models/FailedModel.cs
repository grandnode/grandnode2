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
}
