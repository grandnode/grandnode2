using Grand.Business.Checkout.Services.Payments;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Payments;

[TestClass]
public class PaymentServiceTests
{
    private Mock<IPaymentProvider> _paymentProviderMock;
    private IPaymentService _paymentService;
    private PaymentSettings _paymentSettings;
    private Mock<ISettingService> _settingService;

    [TestInitialize]
    public void TestInitialize()
    {
        _paymentSettings = new PaymentSettings {
            ActivePaymentProviderSystemNames = [
                "Payments.TestMethod"
            ]
        };
        _settingService = new Mock<ISettingService>();
        _paymentProviderMock = new Mock<IPaymentProvider>();
        _paymentService = new PaymentService(_paymentSettings,
            new List<IPaymentProvider> { _paymentProviderMock.Object }, _settingService.Object);
    }

    [TestMethod]
    public void LoadPaymentMethodBySystemName_ReturnPeyment()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var systemName = "systemName";
        var result = _paymentService.LoadPaymentMethodBySystemName(systemName);
        Assert.AreEqual(_paymentProviderMock.Object, result);
    }

    [TestMethod]
    public async Task GetRestrictedCountryIds()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedResult = new List<string> { "1", "2", "3", "4" };
        var expectedKey = "PaymentMethodRestictions.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(It.IsAny<string>(), null, ""))
            .Returns(() => Task.FromResult(new PaymentRestrictedSettings { Ids = expectedResult }));

        var result = await _paymentService.GetRestrictedCountryIds(_paymentProviderMock.Object);
        Assert.IsTrue(expectedResult.SequenceEqual(result));
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, ""), Times.Once);
    }

    [TestMethod]
    public async Task GetRestrictedCountryIds_ReturnEmptyList()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedKey = "PaymentMethodRestictions.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(It.IsAny<string>(), null, ""))
            .Returns(() => Task.FromResult((PaymentRestrictedSettings)null));

        var result = await _paymentService.GetRestrictedCountryIds(_paymentProviderMock.Object);
        Assert.IsEmpty(result);
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, ""), Times.Once);
    }

    [TestMethod]
    public async Task GetRestrictedCountryIds_WithStoreId_PassStoreIdToSettingService()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedResult = new List<string> { "1", "2" };
        var expectedKey = "PaymentMethodRestictions.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(It.IsAny<string>(), null, "storeId"))
            .Returns(() => Task.FromResult(new PaymentRestrictedSettings { Ids = expectedResult }));

        var result = await _paymentService.GetRestrictedCountryIds(_paymentProviderMock.Object, "storeId");
        Assert.IsTrue(expectedResult.SequenceEqual(result));
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, "storeId"),
            Times.Once);
    }

    [TestMethod]
    public async Task GetRestrictedShippingIds_ReturnExpectedIds()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedResult = new List<string> { "Ground", "Pickup" };
        var expectedKey = "PaymentMethodRestictionsShipping.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(It.IsAny<string>(), null, ""))
            .Returns(() => Task.FromResult(new PaymentRestrictedSettings { Ids = expectedResult }));

        var result = await _paymentService.GetRestrictedShippingIds(_paymentProviderMock.Object);
        Assert.IsTrue(expectedResult.SequenceEqual(result));
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, ""), Times.Once);
    }

    [TestMethod]
    public async Task GetRestrictedShippingIds_ReturnEmptyList()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedKey = "PaymentMethodRestictionsShipping.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(It.IsAny<string>(), null, ""))
            .Returns(() => Task.FromResult((PaymentRestrictedSettings)null));

        var result = await _paymentService.GetRestrictedShippingIds(_paymentProviderMock.Object);
        Assert.IsEmpty(result);
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, ""), Times.Once);
    }

    [TestMethod]
    public async Task GetRestrictedShippingIds_WithStoreId_PassStoreIdToSettingService()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedResult = new List<string> { "Ground" };
        var expectedKey = "PaymentMethodRestictionsShipping.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(It.IsAny<string>(), null, "storeId"))
            .Returns(() => Task.FromResult(new PaymentRestrictedSettings { Ids = expectedResult }));

        var result = await _paymentService.GetRestrictedShippingIds(_paymentProviderMock.Object, "storeId");
        Assert.IsTrue(expectedResult.SequenceEqual(result));
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, "storeId"),
            Times.Once);
    }

    [TestMethod]
    public async Task SaveRestictedCountryIds_InvokeSettingsService()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var countryIds = new List<string> { "1", "2", "3", "4" };
        var expectedKey = "PaymentMethodRestictions.systemName";

        await _paymentService.SaveRestrictedCountryIds(_paymentProviderMock.Object, countryIds);
        _settingService.Verify(
            s => s.SetSetting(expectedKey, It.IsAny<PaymentRestrictedSettings>(), It.IsAny<string>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SaveRestictedCountryIds_WithStoreId_SaveSettingForStore()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var countryIds = new List<string> { "1", "2" };
        var expectedKey = "PaymentMethodRestictions.systemName";

        await _paymentService.SaveRestrictedCountryIds(_paymentProviderMock.Object, countryIds, "storeId");
        _settingService.Verify(
            s => s.SetSetting(expectedKey,
                It.Is<PaymentRestrictedSettings>(x => x.Ids.SequenceEqual(countryIds)), "storeId"),
            Times.Once);
    }

    [TestMethod]
    public async Task SaveRestrictedShippingIds_InvokeSettingsService()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var shippingIds = new List<string> { "Ground", "Pickup" };
        var expectedKey = "PaymentMethodRestictionsShipping.systemName";

        await _paymentService.SaveRestrictedShippingIds(_paymentProviderMock.Object, shippingIds);
        _settingService.Verify(
            s => s.SetSetting(expectedKey, It.IsAny<PaymentRestrictedSettings>(), It.IsAny<string>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SaveRestrictedShippingIds_WithStoreId_SaveSettingForStore()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var shippingIds = new List<string> { "Ground" };
        var expectedKey = "PaymentMethodRestictionsShipping.systemName";

        await _paymentService.SaveRestrictedShippingIds(_paymentProviderMock.Object, shippingIds, "storeId");
        _settingService.Verify(
            s => s.SetSetting(expectedKey,
                It.Is<PaymentRestrictedSettings>(x => x.Ids.SequenceEqual(shippingIds)), "storeId"),
            Times.Once);
    }

    [TestMethod]
    public async Task LoadAllPaymentMethods_FilterByCountry_UseStoreScopedRestrictions()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var expectedKey = "PaymentMethodRestictions.systemName";
        _settingService.Setup(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, "storeId"))
            .Returns(() => Task.FromResult(new PaymentRestrictedSettings { Ids = ["countryId"] }));

        var result = await _paymentService.LoadAllPaymentMethods(storeId: "storeId", filterByCountryId: "countryId");

        Assert.IsEmpty(result);
        _settingService.Verify(s => s.GetSettingByKey<PaymentRestrictedSettings>(expectedKey, null, "storeId"),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessPayment_OrderTotalZero_ReturnPaidPaymentStatus()
    {
        var request = new PaymentTransaction {
            TransactionAmount = 0
        };
        var response = await _paymentService.ProcessPayment(request);
        Assert.AreEqual(TransactionStatus.Paid, response.NewPaymentTransactionStatus);
    }

    [TestMethod]
    public async Task ProcessPayment_InvokeProcessPaymentFromPaymentMethod()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var request = new PaymentTransaction { PaymentMethodSystemName = "systemName", TransactionAmount = 500 };


        await _paymentService.ProcessPayment(request);
        _paymentProviderMock.Verify(m => m.ProcessPayment(request), Times.Once);
    }

    [TestMethod]
    public void ProcessPayment_NotFoundPaymentMethod_ThrowException()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName2");
        var request = new PaymentTransaction { PaymentMethodSystemName = "systemName", TransactionAmount = 500 };
        Assert.ThrowsExactlyAsync<GrandException>(async () => await _paymentService.ProcessPayment(request));
    }

    [TestMethod]
    public async Task PostProcessPayment_InvokePostProccessFromPaymentMethod()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var request = new PaymentTransaction {
            PaymentMethodSystemName = "systemName", TransactionAmount = 500,
            TransactionStatus = TransactionStatus.Authorized
        };
        await _paymentService.PostProcessPayment(request);
        _paymentProviderMock.Verify(m => m.PostProcessPayment(request), Times.Once);
    }

    [TestMethod]
    public void PostProcessPayment_NotFoundPaymentMethod_ThrowException()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        var request = new PaymentTransaction {
            PaymentMethodSystemName = "systemName2", TransactionAmount = 500,
            TransactionStatus = TransactionStatus.Authorized
        };
        Assert.ThrowsExactlyAsync<GrandException>(async () => await _paymentService.PostProcessPayment(request),
            "Payment method couldn't be loaded");
    }

    [TestMethod]
    public async Task CanRePostRedirectPayment_ReturnExpectedResult()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        _paymentProviderMock.Setup(c => c.CanRePostRedirectPayment(It.IsAny<PaymentTransaction>())).ReturnsAsync(true);
        var request = new PaymentTransaction { PaymentMethodSystemName = "systemName" };
        _paymentSettings.AllowRePostingPayments = false;
        Assert.IsFalse(await _paymentService.CanRePostRedirectPayment(request));
        _paymentSettings.AllowRePostingPayments = true;
        _paymentProviderMock.Setup(c => c.PaymentMethodType).Returns(PaymentMethodType.Other);
        Assert.IsFalse(await _paymentService.CanRePostRedirectPayment(request));
        request.TransactionStatus = TransactionStatus.Canceled;
        Assert.IsFalse(await _paymentService.CanRePostRedirectPayment(request));
        request.TransactionStatus = TransactionStatus.Pending;
        Assert.IsFalse(await _paymentService.CanRePostRedirectPayment(request));
        _paymentProviderMock.Setup(c => c.PaymentMethodType).Returns(PaymentMethodType.Redirection);
        request.TransactionStatus = TransactionStatus.Pending;
        Assert.IsTrue(await _paymentService.CanRePostRedirectPayment(request));
    }

    [TestMethod]
    public async Task GetAdditionalHandlingFee_ReturnExpectedResults()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        _paymentProviderMock.Setup(c => c.GetAdditionalHandlingFee(It.IsAny<IList<ShoppingCartItem>>()))
            .ReturnsAsync(100);
        var cart = new List<ShoppingCartItem> {
            new(),
            new()
        };
        Assert.AreEqual(100, await _paymentService.GetAdditionalHandlingFee(cart, "systemName"));
    }

    [TestMethod]
    public async Task GetAdditionalHandlingFee_SystemnameNull_ReturnZero()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
        _paymentProviderMock.Setup(c => c.GetAdditionalHandlingFee(It.IsAny<IList<ShoppingCartItem>>()))
            .ReturnsAsync(100);
        var cart = new List<ShoppingCartItem> {
            new(),
            new()
        };
        Assert.AreEqual(0, await _paymentService.GetAdditionalHandlingFee(cart, null));
    }

    [TestMethod]
    public async Task SupportRecurringPayment_ProviderWithoutOverride_IsFalse()
    {
        Assert.IsFalse(await ((IPaymentProvider)new LegacyProvider()).SupportRecurringPayment());
    }

    [TestMethod]
    public async Task CancelRecurringPayment_ProviderWithoutOverride_ReturnsError()
    {
        var result = await ((IPaymentProvider)new LegacyProvider()).CancelRecurringPayment(new RecurringPayment());
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public async Task PaymentService_SupportRecurringPayment_UnknownMethod_IsFalse()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("Payments.Test");
        Assert.IsFalse(await _paymentService.SupportRecurringPayment("Missing"));
    }

    [TestMethod]
    public async Task PaymentService_SupportRecurringPayment_AsksTheProvider()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("Payments.Test");
        _paymentProviderMock.Setup(c => c.SupportRecurringPayment()).ReturnsAsync(true);
        Assert.IsTrue(await _paymentService.SupportRecurringPayment("Payments.Test"));
    }

    [TestMethod]
    public async Task PaymentService_CancelRecurringPayment_UnknownMethod_ReturnsError()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("Payments.Test");

        var result = await _paymentService.CancelRecurringPayment(new RecurringPayment
            { PaymentMethodSystemName = "Missing" });

        Assert.IsFalse(result.Success);
        CollectionAssert.AreEqual(new[] { "Payment method couldn't be loaded" }, result.Errors.ToArray());
    }

    [TestMethod]
    public async Task PaymentService_CancelRecurringPayment_AsksTheProvider()
    {
        _paymentProviderMock.Setup(c => c.SystemName).Returns("Payments.Test");
        _paymentProviderMock.Setup(c => c.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        var result = await _paymentService.CancelRecurringPayment(new RecurringPayment
            { PaymentMethodSystemName = "Payments.Test" });

        Assert.IsTrue(result.Success);
    }

    //a provider written before the recurring contract existed keeps its defaults
    private class LegacyProvider : IPaymentProvider
    {
        public string ConfigurationUrl => throw new NotImplementedException();
        public string SystemName => "Legacy";
        public string FriendlyName => throw new NotImplementedException();
        public int Priority => throw new NotImplementedException();
        public IList<string> LimitedToStores => throw new NotImplementedException();
        public IList<string> LimitedToGroups => throw new NotImplementedException();
        public PaymentMethodType PaymentMethodType => throw new NotImplementedException();
        public string LogoURL => throw new NotImplementedException();
        public Task<PaymentTransaction> InitPaymentTransaction() => throw new NotImplementedException();
        public Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task PostProcessPayment(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task<string> PostRedirectPayment(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart) => throw new NotImplementedException();
        public Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart) => throw new NotImplementedException();
        public Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest) => throw new NotImplementedException();
        public Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task CancelPayment(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction) => throw new NotImplementedException();
        public Task<IList<string>> ValidatePaymentForm(IDictionary<string, string> model) => throw new NotImplementedException();
        public Task<PaymentTransaction> SavePaymentInfo(IDictionary<string, string> model) => throw new NotImplementedException();
        public Task<string> GetControllerRouteName() => throw new NotImplementedException();
        public Task<bool> SkipPaymentInfo() => throw new NotImplementedException();
        public Task<string> Description() => throw new NotImplementedException();
        public Task<bool> SupportCapture() => throw new NotImplementedException();
        public Task<bool> SupportPartiallyRefund() => throw new NotImplementedException();
        public Task<bool> SupportRefund() => throw new NotImplementedException();
        public Task<bool> SupportVoid() => throw new NotImplementedException();
    }
}