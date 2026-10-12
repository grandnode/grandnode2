using Authentication.OpenIdConnect;
using Authentication.OpenIdConnect.Infrastructure;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcTokenRefresherTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        public string LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return await answer(request);
        }
    }

    private StubHandler _handler;
    private OpenIdConnectOptions _options;
    private OidcLoginConfig _config;

    [TestInitialize]
    public void Init()
    {
        _options = new OpenIdConnectOptions {
            ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                new OpenIdConnectConfiguration { TokenEndpoint = "https://idp.test/token" })
        };
        _config = new OidcLoginConfig { ClientId = "shop", ClientSecret = "secret" };
        Answer(HttpStatusCode.OK, "{\"access_token\":\"at\",\"refresh_token\":\"rt-2\"}");
    }

    private void Answer(HttpStatusCode status, string json) =>
        _handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(status) {
            Content = new StringContent(json ?? "", System.Text.Encoding.UTF8, "application/json")
        }));

    private OidcTokenRefresher Refresher()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(OidcLoginDefaults.HttpClientName)).Returns(() => new HttpClient(_handler));
        var monitor = new Mock<IOptionsMonitor<OpenIdConnectOptions>>();
        monitor.Setup(x => x.Get(OidcLoginDefaults.AuthenticationScheme)).Returns(_options);
        return new OidcTokenRefresher(factory.Object, monitor.Object, _config, NullLogger<OidcTokenRefresher>.Instance);
    }

    [TestMethod]
    public async Task Ok_Rotated()
    {
        var result = await Refresher().Refresh("rt-1");
        Assert.AreEqual(OidcRefreshOutcome.Refreshed, result.Outcome);
        Assert.AreEqual("rt-2", result.RefreshToken);
        StringAssert.Contains(_handler.LastBody, "grant_type=refresh_token");
        StringAssert.Contains(_handler.LastBody, "refresh_token=rt-1");
        StringAssert.Contains(_handler.LastBody, "client_id=shop");
        StringAssert.Contains(_handler.LastBody, "client_secret=secret");
    }

    [TestMethod]
    public async Task Ok_NotRotated_NullToken()
    {
        Answer(HttpStatusCode.OK, "{\"access_token\":\"at\"}");
        var result = await Refresher().Refresh("rt-1");
        Assert.AreEqual(OidcRefreshOutcome.Refreshed, result.Outcome);
        Assert.IsNull(result.RefreshToken);
    }

    [TestMethod]
    public async Task PublicClient_NoSecretSent()
    {
        _config.ClientSecret = "";
        await Refresher().Refresh("rt-1");
        Assert.IsFalse(_handler.LastBody.Contains("client_secret"));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\"}")]
    [DataRow(HttpStatusCode.Unauthorized, "{\"error\":\"invalid_client\"}")]
    public async Task ClientErrors_Refused(HttpStatusCode status, string json)
    {
        Answer(status, json);
        Assert.AreEqual(OidcRefreshOutcome.Refused, (await Refresher().Refresh("rt-1")).Outcome);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    public async Task OtherStatus_Unavailable(HttpStatusCode status)
    {
        Answer(status, "");
        Assert.AreEqual(OidcRefreshOutcome.Unavailable, (await Refresher().Refresh("rt-1")).Outcome);
    }

    [TestMethod]
    public async Task NetworkError_Unavailable()
    {
        _handler = new StubHandler(_ => throw new HttpRequestException("down"));
        Assert.AreEqual(OidcRefreshOutcome.Unavailable, (await Refresher().Refresh("rt-1")).Outcome);
    }

    [TestMethod]
    public async Task Timeout_Unavailable()
    {
        _handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));
        Assert.AreEqual(OidcRefreshOutcome.Unavailable, (await Refresher().Refresh("rt-1")).Outcome);
    }

    [TestMethod]
    public async Task InvalidJson_Unavailable()
    {
        Answer(HttpStatusCode.OK, "<html>");
        Assert.AreEqual(OidcRefreshOutcome.Unavailable, (await Refresher().Refresh("rt-1")).Outcome);
    }

    [TestMethod]
    public async Task MetadataUnobtainable_Unavailable()
    {
        var manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        manager.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("IDX20803"));
        _options.ConfigurationManager = manager.Object;
        Assert.AreEqual(OidcRefreshOutcome.Unavailable, (await Refresher().Refresh("rt-1")).Outcome);
    }

    //no sign-in scheme (not configured): nothing can ever confirm the session
    [TestMethod]
    public async Task NotConfigured_Refused()
    {
        _options.ConfigurationManager = null;
        Assert.AreEqual(OidcRefreshOutcome.Refused, (await Refresher().Refresh("rt-1")).Outcome);
    }
}
