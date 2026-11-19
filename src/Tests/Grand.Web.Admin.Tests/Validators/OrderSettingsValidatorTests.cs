using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.AdminShared.Validators.Settings;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class OrderSettingsValidatorTests
{
    private readonly OrderSettingsValidator _validator =
        new(new List<IValidatorConsumer<SalesSettingsModel.OrderSettingsModel>>(), Translations());

    private static IRequestTranslationService Translations()
    {
        var mock = new Mock<IRequestTranslationService>();
        mock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("message");
        return mock.Object;
    }

    private static SalesSettingsModel.OrderSettingsModel Model(int paymentReminderDaysBefore) => new() {
        GiftVouchers_Activated_OrderStatusId = (int)OrderStatusSystem.Complete,
        PaymentReminderDaysBefore = paymentReminderDaysBefore
    };

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    public async Task PaymentReminderDaysBefore_ZeroOrMore_IsValid(int days)
    {
        Assert.IsTrue((await _validator.ValidateAsync(Model(days))).IsValid);
    }

    [TestMethod]
    public async Task PaymentReminderDaysBefore_Negative_IsInvalid()
    {
        var result = await _validator.ValidateAsync(Model(-1));

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(nameof(SalesSettingsModel.OrderSettingsModel.PaymentReminderDaysBefore),
            result.Errors.Single().PropertyName);
    }
}
