using Grand.Business.Core.Utilities.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Authentication.OpenIdConnect.Infrastructure;

/// <summary>
///     What the server-side session needs from the provider sign-in (refresh token, provider session id), carried from
///     the token response to the callback in the external authentication cookie next to the pending identity
/// </summary>
public static class OidcLoginSessionData
{
    public const string RefreshTokenItem = ".Grand.OidcLogin.RefreshToken";
    public const string SidItem = ".Grand.OidcLogin.Sid";

    public static void Capture(AuthenticationProperties properties, OpenIdConnectMessage tokenResponse)
    {
        if (properties == null || tokenResponse == null)
            return;
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            properties.Items[RefreshTokenItem] = tokenResponse.RefreshToken;
        var sid = Sid(tokenResponse.IdToken);
        if (sid != null)
            properties.Items[SidItem] = sid;
    }

    /// <summary>The sid claim of an ID token already validated by the handler; null when absent or unreadable</summary>
    public static string Sid(string idToken)
    {
        if (string.IsNullOrEmpty(idToken))
            return null;
        try
        {
            return new JsonWebToken(idToken).TryGetPayloadValue<string>("sid", out var sid) && !string.IsNullOrEmpty(sid)
                ? sid
                : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static async Task<(string RefreshToken, string Sid)> Read(HttpContext context)
    {
        var result = await context.AuthenticateAsync(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme);
        var items = result?.Succeeded == true ? result.Properties?.Items : null;
        string refreshToken = null, sid = null;
        items?.TryGetValue(RefreshTokenItem, out refreshToken);
        items?.TryGetValue(SidItem, out sid);
        return (refreshToken, sid);
    }
}
