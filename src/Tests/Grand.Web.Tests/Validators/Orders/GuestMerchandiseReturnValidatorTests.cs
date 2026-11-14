using Grand.Domain.Common;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Localization;
using Grand.Web.Models.Orders;
using Grand.Web.Validators.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Orders;

[TestClass]
public class GuestMerchandiseReturnValidatorTests
{
    private static GuestMerchandiseReturnValidator Validator(CaptchaSettings captchaSettings = null)
    {
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns<string>(key => key);
        return new GuestMerchandiseReturnValidator(
            new List<IValidatorConsumer<GuestMerchandiseReturnModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            captchaSettings ?? new CaptchaSettings(),
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IGoogleReCaptchaValidator>().Object,
            translation.Object);
    }

    private static GuestMerchandiseReturnModel Model()
    {
        return new GuestMerchandiseReturnModel { OrderCode = "K7Q2M9XA", Email = "guest@example.com" };
    }

    [TestMethod]
    public async Task Validator_Valid_NoErrors()
    {
        Assert.IsTrue((await Validator().ValidateAsync(Model())).IsValid);
    }

    [TestMethod]
    public async Task Validator_OrderCodeMissing_Error()
    {
        var model = Model();
        model.OrderCode = "";

        var result = await Validator().ValidateAsync(model);

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.Guest.OrderCode.Required"));
    }

    [TestMethod]
    public async Task Validator_EmailMissing_Error()
    {
        var model = Model();
        model.Email = "";

        var result = await Validator().ValidateAsync(model);

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.Guest.Email.Required"));
    }

    [TestMethod]
    public async Task Validator_EmailMalformed_Error()
    {
        var model = Model();
        model.Email = "not-an-address";

        var result = await Validator().ValidateAsync(model);

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Common.WrongEmail"));
    }

    [TestMethod]
    public async Task Validator_CaptchaOn_NoCaptcha_Refused()
    {
        var model = Model();
        model.Captcha = null;

        var result = await Validator(new CaptchaSettings { Enabled = true, ShowOnGuestMerchandiseReturnPage = true })
            .ValidateAsync(model);

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "Account.Captcha.Required"));
    }

    [TestMethod]
    public async Task Validator_CaptchaOff_NoCaptcha_Valid()
    {
        var model = Model();
        model.Captcha = null;

        var result = await Validator(new CaptchaSettings { Enabled = true }).ValidateAsync(model);

        Assert.IsTrue(result.IsValid);
    }
}
