using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Authentication.OpenIdConnect.Infrastructure;

/// <summary>
///     Validates a logout token of OpenID Connect Back-Channel Logout 1.0
/// </summary>
public class OidcLogoutTokenValidator(IMemoryCache cache)
{
    public const string BackChannelLogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

    /// <summary>A logout token older than this is refused (it is sent at once by the provider)</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

    private static readonly string[] AsymmetricAlgorithms = [
        SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
        SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512
    ];

    public static bool TypeAccepted(string typ) =>
        string.IsNullOrEmpty(typ) || typ.Equals("logout+jwt", StringComparison.OrdinalIgnoreCase) ||
        typ.Equals("JWT", StringComparison.OrdinalIgnoreCase);

    public async Task<OidcLogoutToken> Validate(string token, OpenIdConnectConfiguration configuration, string clientId,
        TimeSpan clockSkew)
    {
        if (string.IsNullOrEmpty(token) || configuration == null || string.IsNullOrEmpty(clientId))
            return OidcLogoutToken.Invalid("The logout token is missing");

        var parameters = new TokenValidationParameters {
            ValidIssuer = configuration.Issuer,
            ValidAudience = clientId,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidAlgorithms = AsymmetricAlgorithms,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            //exp is optional in a logout token; when present it is checked
            RequireExpirationTime = false,
            ValidateLifetime = true,
            ClockSkew = clockSkew,
            TypeValidator = (type, securityToken, _) =>
                TypeAccepted(type) ? type : throw new SecurityTokenInvalidTypeException("The token type is not accepted")
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt)
            return OidcLogoutToken.Invalid("The logout token is invalid");

        if (!HasBackChannelEvent(jwt))
            return OidcLogoutToken.Invalid("The logout token has no back-channel logout event");
        if (jwt.TryGetPayloadValue<object>("nonce", out _))
            return OidcLogoutToken.Invalid("A logout token must not have a nonce");

        var now = DateTime.UtcNow;
        if (jwt.IssuedAt == DateTime.MinValue || jwt.IssuedAt > now + clockSkew || now - jwt.IssuedAt > MaxAge + clockSkew)
            return OidcLogoutToken.Invalid("The logout token is not recent");

        var subject = jwt.TryGetPayloadValue<string>("sub", out var sub) && !string.IsNullOrEmpty(sub) ? sub : null;
        var sid = jwt.TryGetPayloadValue<string>("sid", out var sidValue) && !string.IsNullOrEmpty(sidValue) ? sidValue : null;
        if (subject == null && sid == null)
            return OidcLogoutToken.Invalid("The logout token names no subject and no session");

        if (!string.IsNullOrEmpty(jwt.Id))
        {
            var key = "Grand.OidcLogin.LogoutJti." + jwt.Issuer + "|" + jwt.Id;
            if (cache.TryGetValue(key, out _))
                return OidcLogoutToken.Invalid("The logout token was already used");
            var until = (jwt.ValidTo != DateTime.MinValue ? jwt.ValidTo : jwt.IssuedAt + MaxAge) + clockSkew;
            cache.Set(key, true, new DateTimeOffset(DateTime.SpecifyKind(until, DateTimeKind.Utc)));
        }

        return OidcLogoutToken.Valid(subject, sid);
    }

    private static bool HasBackChannelEvent(JsonWebToken jwt)
    {
        var events = jwt.Claims.FirstOrDefault(c => c.Type == "events")?.Value;
        if (string.IsNullOrEmpty(events))
            return false;
        try
        {
            using var document = JsonDocument.Parse(events);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty(BackChannelLogoutEvent, out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
