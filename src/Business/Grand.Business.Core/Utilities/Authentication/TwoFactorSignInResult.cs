namespace Grand.Business.Core.Utilities.Authentication;

/// <summary>
///     The answer to the code of a sign-in waiting for its second step
/// </summary>
public enum TwoFactorSignInResult
{
    Passed,
    WrongCode,

    /// <summary>
    ///     The customer is locked out, also when this code was the last wrong one allowed: the sign-in starts again later
    /// </summary>
    LockedOut
}
