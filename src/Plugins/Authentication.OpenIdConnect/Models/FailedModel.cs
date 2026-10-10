using Grand.Business.Core.Utilities.Authentication;

namespace Authentication.OpenIdConnect.Models;

public class FailedModel
{
    public OidcLoginRefusal Reason { get; set; }
    public bool Mismatch { get; set; }
}
