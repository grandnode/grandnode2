using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Grand.Web.Common.Tests.Security.Oidc;

/// <summary>
///     Challenges and refusals on MVC controller endpoints, registered like the app does (MVC before AddProblemDetails)
/// </summary>
[TestClass]
public class OidcMvcProblemDetailsTests
{
    private OidcTestHost _host;
    private HttpClient _client;

    [TestInitialize]
    public async Task Init()
    {
        _host = new OidcTestHost();
        _client = await _host.Start(OidcTestHost.Config(),
            s => s.AddControllers().AddApplicationPart(typeof(OidcProbeController).Assembly),
            endpoints: e => e.MapControllers());
    }

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    [TestMethod]
    [DataRow("/api/probe", null, 401)]
    [DataRow("/api/probe", "not-a-token", 401)]
    [DataRow("/api/probe", "scope:other", 403)]
    [DataRow("/api/apiprobe", null, 401)]
    [DataRow("/api/apiprobe", "scope:other", 403)]
    public async Task ChallengeAndRefusal_WriteProblemDetailsBody(string path, string token, int status)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                token == "scope:other" ? OidcTestHost.Token(scope: "other") : token);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(status, (int)response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType, body);
        using var json = JsonDocument.Parse(body);
        Assert.AreEqual(status, json.RootElement.GetProperty("status").GetInt32());
    }
}

/// <summary>
///     Like the Web API controllers: a controller without [ApiController]
/// </summary>
[Authorize(AuthenticationSchemes = "Oidc")]
[Route("api/probe")]
[Produces("application/json")]
public class OidcProbeController : ControllerBase
{
    [HttpGet]
    public string Get() => "ok";
}

[ApiController]
[Authorize(AuthenticationSchemes = "Oidc")]
[Route("api/apiprobe")]
[Produces("application/json")]
public class OidcApiProbeController : ControllerBase
{
    [HttpGet]
    public string Get() => "ok";
}
