using Grand.Infrastructure.Configuration;
using Grand.Module.Scim.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Scim;

[TestClass]
public class ScimAuthorizeFilterTests
{
    private const string Token = "0123456789abcdef0123456789abcdef";

    private ScimConfig _config;
    private OidcLoginConfig _loginConfig;
    private OidcResourceServerConfig _resourceServer;

    [TestInitialize]
    public void Init()
    {
        _config = new ScimConfig { Token = Token };
        _loginConfig = new OidcLoginConfig { ClientId = "grandnode" };
        _resourceServer = new OidcResourceServerConfig { Authority = "https://idp.example.com" };
    }

    private AuthorizationFilterContext Run(string authorization)
    {
        var httpContext = new DefaultHttpContext();
        if (authorization != null)
            httpContext.Request.Headers.Authorization = authorization;
        var context = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), []);
        new ScimAuthorizeFilter(_config, _loginConfig, _resourceServer, NullLogger<ScimAuthorizeFilter>.Instance)
            .OnAuthorization(context);
        return context;
    }

    private static void AssertUnauthorized(AuthorizationFilterContext context) =>
        Assert.AreEqual(StatusCodes.Status401Unauthorized, (context.Result as ContentResult)?.StatusCode);

    [TestMethod]
    public void RightToken_Passes() => Assert.IsNull(Run("Bearer " + Token).Result);

    [TestMethod]
    public void SchemeCaseInsensitive_Passes() => Assert.IsNull(Run("bearer " + Token).Result);

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Bearer")]
    [DataRow("Basic " + Token)]
    [DataRow("Bearer 0123456789abcdef0123456789abcdeX")]
    [DataRow("Bearer " + Token + "x")]
    public void WrongOrMissingToken_Unauthorized(string authorization) => AssertUnauthorized(Run(authorization));

    //a short secret is a configuration mistake: nobody gets in, even with that secret
    [TestMethod]
    public void ShortConfiguredToken_Unauthorized()
    {
        _config.Token = "short";
        AssertUnauthorized(Run("Bearer short"));
    }

    //accounts without a password and without a provider would be useless: the endpoint stays shut
    [TestMethod]
    public void OidcLoginNotConfigured_Unauthorized()
    {
        _loginConfig.ClientId = "";
        AssertUnauthorized(Run("Bearer " + Token));
    }
}
