using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Grand.Web.Common.Tests.Security.Oidc;

/// <summary>
///     Minimal host running the real JwtBearer handler against an in-memory signing key - no network
/// </summary>
public sealed class OidcTestHost : IAsyncDisposable
{
    public const string Issuer = "https://idp.test/realms/shop";
    public static readonly RsaSecurityKey IdpKey = new(RSA.Create(2048)) { KeyId = "idp-1" };
    public static readonly RsaSecurityKey ForeignKey = new(RSA.Create(2048)) { KeyId = "idp-1" };

    private IHost _host;

    public static string Token(string audience = "http://localhost/api", string scope = "grandnode.api",
        SecurityKey key = null, string algorithm = SecurityAlgorithms.RsaSha256, DateTime? expires = null,
        string issuer = Issuer, params Claim[] extra)
    {
        var claims = new List<Claim> {
            new("sub", "sub-1"), new("email", "john@example.com"), new("email_verified", "true", ClaimValueTypes.Boolean)
        };
        if (scope != null) claims.Add(new Claim("scope", scope));
        claims.AddRange(extra);
        var exp = expires ?? DateTime.UtcNow.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = exp.AddMinutes(-20),
            IssuedAt = exp.AddMinutes(-20),
            Expires = exp,
            SigningCredentials = new SigningCredentials(key ?? IdpKey, algorithm)
        });
    }

    public static OidcResourceServerConfig Config(params OidcProtectedResource[] resources) => new() {
        Enabled = true,
        Authority = Issuer,
        Resources = resources.Length > 0
            ? [.. resources]
            : [new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api", ScopesSupported = ["grandnode.api"], RequiredScopes = ["grandnode.api"] }]
    };

    public static Task<HttpResponseMessage> Send(HttpClient client, string path, string token = null, string scheme = "Bearer")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue(scheme, token);
        return client.SendAsync(request);
    }

    public async Task<HttpClient> Start(OidcResourceServerConfig config,
        Action<IServiceCollection> services = null,
        Action<AuthenticationBuilder> auth = null,
        Action<IEndpointRouteBuilder> endpoints = null)
    {
        _host = await new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(s =>
            {
                s.AddSingleton(config);
                s.AddSingleton<IOidcProtectedResourceRegistry, OidcProtectedResourceRegistry>();
                s.AddRouting();
                s.AddAuthorization();
                //before AddProblemDetails, as in the app: MVC is registered first, so its problem details writer comes first
                services?.Invoke(s);
                s.AddProblemDetails();
                s.TryAddSingleton(StoreContextSetter("http://localhost/"));
                //defaults for what the Oidc events need; a test registers its own mock before these to override
                var defaultResolver = new Mock<IOidcCustomerResolver>();
                defaultResolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .ReturnsAsync(OidcCustomerResult.Success(new Customer { Id = "c1", Active = true }));
                s.TryAddSingleton(defaultResolver.Object);
                var permissions = new Mock<IPermissionService>();
                permissions.Setup(x => x.Authorize(It.IsAny<Permission>(), It.IsAny<Customer>())).ReturnsAsync(true);
                s.TryAddSingleton(permissions.Object);
                s.TryAddSingleton(new Mock<IGroupService>().Object);
                s.TryAddScoped<OidcResourceAccess>();
                s.AddHttpContextAccessor();
                s.TryAddScoped<IRequestCustomerResolver, OidcRequestCustomerResolver>();
                var builder = s.AddAuthentication();
                auth?.Invoke(builder);
                builder.AddOidcResourceServer(config, o =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    configuration.SigningKeys.Add(IdpKey);
                    o.Configuration = configuration;
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(e =>
                {
                    OidcProtectedResourceMetadata.Map(e);
                    //a plugin-style endpoint protected directly by the Oidc scheme
                    e.MapGet("/api/ping", (HttpContext c) => c.User.FindFirst("sub")?.Value ?? "anonymous")
                        .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = OidcResourceServerDefaults.AuthenticationScheme });
                    e.MapGet("/other/ping", () => "ok")
                        .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = OidcResourceServerDefaults.AuthenticationScheme });
                    endpoints?.Invoke(e);
                });
            })).StartAsync();
        return _host.GetTestClient();
    }

    public static IStoreContextSetter StoreContextSetter(string url)
    {
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(c => c.CurrentStore).Returns(new Store { Id = "store1", Url = url });
        var setter = new Mock<IStoreContextSetter>();
        setter.Setup(s => s.InitializeStoreContext(It.IsAny<string>())).ReturnsAsync(storeContext.Object);
        return setter.Object;
    }

    public async ValueTask DisposeAsync()
    {
        if (_host == null) return;
        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }
}
