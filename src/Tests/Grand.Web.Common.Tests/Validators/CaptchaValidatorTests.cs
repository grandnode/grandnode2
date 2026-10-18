using Grand.Infrastructure.Models;
using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Validators;

[TestClass]
public class CaptchaValidatorTests
{
    private sealed class CaptchaModel : ICaptchaValidModel
    {
        public string ReCaptchaChallengeField { get; set; }
        public string ReCaptchaResponseField { get; set; }
        public string ReCaptchaResponseValue { get; set; }
        public string ReCaptchaResponse { get; set; }
    }

    [TestMethod]
    public async Task Validate_PassesRequestAbortedToGoogle()
    {
        using var aborted = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext { RequestAborted = aborted.Token };
        httpContext.Request.ContentType = "application/json";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);
        var google = new Mock<IGoogleReCaptchaValidator>();
        google.Setup(x => x.Validate("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleReCaptchaResponse { Success = true });
        var validator = new CaptchaValidator([], accessor.Object, google.Object);

        var result = await validator.ValidateAsync(new CaptchaModel { ReCaptchaResponse = "token" });

        Assert.IsTrue(result.IsValid);
        google.Verify(x => x.Validate("token", aborted.Token), Times.Once);
    }
}
