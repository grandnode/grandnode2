namespace Grand.Business.Core.Utilities.Authentication;

public enum OidcFailureKind
{
    /// <summary>401 error="invalid_token"</summary>
    InvalidToken,

    /// <summary>403 error="insufficient_scope"</summary>
    InsufficientScope,

    /// <summary>403 without a bearer error</summary>
    Forbidden,

    /// <summary>503 without a bearer error: the authorization server's metadata or signing keys cannot be obtained</summary>
    ServiceUnavailable
}
