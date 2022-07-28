using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Extensions
{
    [TestClass()]
    public class PaymentExtensionsTests
    {
        private Mock<IPaymentProvider> _providerMock;

        [TestInitialize]
        public void Init()
        {
            _providerMock = new Mock<IPaymentProvider>();
        }

        [TestMethod()]
        public void IsPaymentMethodActive_ReturnExpectedResult()
        {
            var settings = new PaymentSettings();
            _providerMock.Setup(c => c.SystemName).Returns("paypal");
            settings.ActivePaymentProviderSystemNames.Add("paypal");
            Assert.IsTrue(_providerMock.Object.IsPaymentMethodActive(settings));
            _providerMock.Setup(c => c.SystemName).Returns("klarna");
            Assert.IsFalse(_providerMock.Object.IsPaymentMethodActive(settings));
        }

        [TestMethod()]
        public void IsPaymentMethodActive_NullSettings_ThrowException()
        {
            PaymentSettings settings = null;
            Assert.ThrowsException<ArgumentNullException>(() => _providerMock.Object.IsPaymentMethodActive(settings));
        }

        [TestMethod()]
        public void IsPaymentMethodActive_NullProvider_ThrowException()
        {
            IPaymentProvider provider = null;
            var settings = new PaymentSettings();
            Assert.ThrowsException<ArgumentNullException>(() => provider.IsPaymentMethodActive(settings));
        }
    }
}
