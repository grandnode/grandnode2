using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Authentication.OpenIdConnect.Infrastructure;

/// <summary>
///     Confirms a session signed in through the provider at the identity provider by exchanging its refresh token (RFC 6749, section 6)
/// </summary>
public class OidcTokenRefresher(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> optionsMonitor,
    OidcLoginConfig config,
    ILogger<OidcTokenRefresher> logger) : IOidcTokenRefresher
{
    public async Task<OidcRefreshResult> Refresh(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        var options = optionsMonitor.Get(OidcLoginDefaults.AuthenticationScheme);
        if (options.ConfigurationManager == null)
        {
            logger.LogWarning("OpenID Connect login is not configured: sessions signed in through the provider cannot be confirmed");
            return OidcRefreshResult.Refused();
        }

        try
        {
            var configuration = await options.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            if (string.IsNullOrEmpty(configuration.TokenEndpoint))
            {
                logger.LogWarning("The identity provider metadata has no token endpoint");
                return OidcRefreshResult.Unavailable();
            }

            var form = new Dictionary<string, string> {
                ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken, ["client_id"] = config.ClientId
            };
            if (!string.IsNullOrEmpty(config.ClientSecret))
                form["client_secret"] = config.ClientSecret;

            using var client = httpClientFactory.CreateClient(OidcLoginDefaults.HttpClientName);
            using var response = await client.PostAsync(configuration.TokenEndpoint, new FormUrlEncodedContent(form),
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                //the error code only, never the body: it may echo the token
                logger.LogInformation("The identity provider refused a refresh token: {Status} {Error}",
                    (int)response.StatusCode, ErrorCode(body));
                return OidcRefreshResult.Refused();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("The identity provider token endpoint answered {Status}", (int)response.StatusCode);
                return OidcRefreshResult.Unavailable();
            }

            using var document = JsonDocument.Parse(body);
            var rotated = document.RootElement.TryGetProperty("refresh_token", out var value) &&
                          value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
            return OidcRefreshResult.Refreshed(rotated);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or InvalidOperationException or IOException)
        {
            logger.LogWarning("The identity provider could not be reached to confirm a session ({Exception})", ex.GetType().Name);
            return OidcRefreshResult.Unavailable();
        }
    }

    private static string ErrorCode(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
