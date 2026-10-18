using Grand.Domain.Common;
using Grand.Web.Common.Security.Captcha;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;

namespace Grand.Web.Common.Tests.Security.Captcha;

[TestClass]
public class GoogleReCaptchaValidatorTests
{
    private sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => answer(cancellationToken);
    }

    private static GoogleReCaptchaValidator Validator(HttpMessageHandler handler)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        return new GoogleReCaptchaValidator(new CaptchaSettings { ReCaptchaPrivateKey = "secret" }, accessor.Object,
            new HttpClient(handler));
    }

    [TestMethod]
    public async Task Validate_GoogleConfirms_Success()
    {
        var validator = Validator(new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"success\":true}")
        })));

        var result = await validator.Validate("token");

        Assert.IsTrue(result.Success);
    }

    //the visitor left: the call to Google stops instead of holding the request until the client timeout
    [TestMethod]
    public async Task Validate_Cancelled_FailsWithoutWaitingForGoogle()
    {
        var validator = Validator(new StubHandler(async ct => {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var result = await validator.Validate("token", cts.Token).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsFalse(result.Success);
    }
}
