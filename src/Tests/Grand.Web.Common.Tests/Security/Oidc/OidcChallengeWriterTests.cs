using Grand.Business.Core.Utilities.Authentication;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcChallengeWriterTests
{
    private const string Meta = "https://shop.example.com/.well-known/oauth-protected-resource/api";

    [TestMethod]
    public void NoFailure_MetadataAndScope() =>
        Assert.AreEqual($"Bearer resource_metadata=\"{Meta}\", scope=\"grandnode.api\"",
            OidcChallengeWriter.BuildHeader(null, Meta, "grandnode.api"));

    [TestMethod]
    public void NoFailure_NoScope() =>
        Assert.AreEqual($"Bearer resource_metadata=\"{Meta}\"", OidcChallengeWriter.BuildHeader(null, Meta, null));

    [TestMethod]
    public void InvalidToken() =>
        Assert.AreEqual($"Bearer error=\"invalid_token\", error_description=\"The token has expired\", resource_metadata=\"{Meta}\"",
            OidcChallengeWriter.BuildHeader(
                new OidcAuthenticationFailure { Kind = OidcFailureKind.InvalidToken, Description = "The token has expired" }, Meta, "ignored"));

    [TestMethod]
    public void InsufficientScope_UsesFailureScope() =>
        Assert.AreEqual($"Bearer error=\"insufficient_scope\", error_description=\"The token lacks a required scope\", scope=\"a b\", resource_metadata=\"{Meta}\"",
            OidcChallengeWriter.BuildHeader(new OidcAuthenticationFailure {
                Kind = OidcFailureKind.InsufficientScope, Description = "The token lacks a required scope", Scope = "a b"
            }, Meta, "ignored"));

    [TestMethod]
    public void QuotesEscaped_ControlCharactersDropped() =>
        Assert.AreEqual("Bearer error=\"invalid_token\", error_description=\"a\\\"b\\\\cd\"",
            OidcChallengeWriter.BuildHeader(
                new OidcAuthenticationFailure { Kind = OidcFailureKind.InvalidToken, Description = "a\"b\\c\r\nd" }, null, null));

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        return context;
    }

    [TestMethod]
    public async Task Write_Forbidden_403_NoHeader_ProblemJson()
    {
        var context = Context();
        await OidcChallengeWriter.Write(context,
            new OidcAuthenticationFailure { Kind = OidcFailureKind.Forbidden, Description = "The customer account is inactive" }, Meta, null);

        Assert.AreEqual(403, context.Response.StatusCode);
        Assert.IsFalse(context.Response.Headers.ContainsKey("Retry-After"));
        Assert.IsFalse(context.Response.Headers.ContainsKey("WWW-Authenticate"));
        StringAssert.StartsWith(context.Response.ContentType, "application/problem+json");
    }

    [TestMethod]
    public async Task Write_NoFailure_401_WithHeader()
    {
        var context = Context();
        await OidcChallengeWriter.Write(context, null, Meta, "grandnode.api");

        Assert.AreEqual(401, context.Response.StatusCode);
        StringAssert.Contains(context.Response.Headers.WWWAuthenticate.ToString(), "resource_metadata");
    }

    [TestMethod]
    public async Task Write_ServiceUnavailable_503_NoBearerError_ProblemJson()
    {
        var context = Context();
        await OidcChallengeWriter.Write(context,
            new OidcAuthenticationFailure { Kind = OidcFailureKind.ServiceUnavailable, Description = "The authorization server is unavailable" }, Meta, null);

        Assert.AreEqual(503, context.Response.StatusCode);
        Assert.AreEqual("30", context.Response.Headers.RetryAfter.ToString());
        Assert.IsFalse(context.Response.Headers.ContainsKey("WWW-Authenticate"));
        StringAssert.StartsWith(context.Response.ContentType, "application/problem+json");
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        StringAssert.Contains(body, "\"status\":503");
        StringAssert.Contains(body, "\"title\":\"Service unavailable\"");
    }
}
