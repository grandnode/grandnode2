using Grand.Web.Common.Middleware;
using Grand.Web.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Tests.Endpoints;

[TestClass]
public class OidcLoginEndpointProviderTests
{
    //the identity providers have these addresses registered: they never move, and never get a language prefix
    [TestMethod]
    public void RegisterEndpoint_RegistersOidcRoutesAtRoot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllersWithViews();
        var application = builder.Build();

        new OidcLoginEndpointProvider().RegisterEndpoint(application);

        var routes = ((IEndpointRouteBuilder)application).DataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>()
            .ToList();
        var named = routes.Select(x => (x.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName, x.RoutePattern.RawText))
            .Where(x => x.RouteName != null).ToList();
        CollectionAssert.IsSubsetOf(new[] {
            ("OidcLoginLogin", "oidc/login"),
            ("OidcLoginCallback", "oidc/callback"),
            ("OidcLoginLink", "oidc/link"),
            ("OidcLoginLinkComplete", "oidc/link/complete"),
            ("OidcLoginFailed", "oidc/failed")
        }, named);

        var backChannel = routes.Single(x => x.RoutePattern.RawText == "oidc/backchannel-logout");
        CollectionAssert.AreEqual(new[] { "POST" },
            backChannel.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ToArray());
        Assert.IsNotNull(backChannel.Metadata.GetMetadata<NoRequestContextMetadata>());
    }
}
