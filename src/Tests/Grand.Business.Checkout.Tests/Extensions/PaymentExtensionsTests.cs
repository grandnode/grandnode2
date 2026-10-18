using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class PaymentExtensionsTests
{
    private Mock<IPaymentProvider> _providerMock;

    [TestInitialize]
    public void Init()
    {
        _providerMock = new Mock<IPaymentProvider>();
    }

    [TestMethod]
    public void IsPaymentMethodActive_ReturnExpectedResult()
    {
        var settings = new PaymentSettings();
        _providerMock.Setup(c => c.SystemName).Returns("stripe");
        settings.ActivePaymentProviderSystemNames.Add("stripe");
        Assert.IsTrue(_providerMock.Object.IsPaymentMethodActive(settings));
        _providerMock.Setup(c => c.SystemName).Returns("klarna");
        Assert.IsFalse(_providerMock.Object.IsPaymentMethodActive(settings));
    }

    [TestMethod]
    public void IsPaymentMethodActive_NullSettings_ThrowException()
    {
        PaymentSettings settings = null;
        Assert.ThrowsExactly<ArgumentNullException>(() => _providerMock.Object.IsPaymentMethodActive(settings));
    }

    [TestMethod]
    public void IsPaymentMethodActive_NullProvider_ThrowException()
    {
        IPaymentProvider provider = null;
        var settings = new PaymentSettings();
        Assert.ThrowsExactly<ArgumentNullException>(() => provider.IsPaymentMethodActive(settings));
    }

    [TestMethod]
    public void GetPaymentMethodName_OrderLanguage_ReturnNameInOrderLanguage()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("Request language");
        _providerMock.Setup(c => c.GetFriendlyName("1")).Returns("Order language");
        var order = new Order { CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Order language", _providerMock.Object.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void GetPaymentMethodName_NoOrderLanguage_ReturnFriendlyName()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("Request language");
        var order = new Order { CustomerLanguageId = null, PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Request language", _providerMock.Object.GetPaymentMethodName(order));
        _providerMock.Verify(c => c.GetFriendlyName(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void GetPaymentMethodName_NoTranslation_ReturnFriendlyName()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("Request language");
        _providerMock.Setup(c => c.GetFriendlyName("1")).Returns("");
        var order = new Order { CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Request language", _providerMock.Object.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void GetPaymentMethodName_NoName_ReturnSystemName()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("");
        _providerMock.Setup(c => c.GetFriendlyName("1")).Returns("");
        var order = new Order { CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Payments.Test", _providerMock.Object.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void GetPaymentMethodName_NullProvider_ReturnSystemName()
    {
        IPaymentProvider provider = null;
        var order = new Order { PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Payments.Test", provider.GetPaymentMethodName(order));
    }
}
