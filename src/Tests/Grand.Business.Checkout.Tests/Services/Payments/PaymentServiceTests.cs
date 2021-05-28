using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Services.Payments;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Services.Payments
{
    [TestClass()]
    public class PaymentServiceTests
    {
        private PaymentSettings _paymentSettings;
        private Mock<ISettingService> _settingService;
        private Mock<IPaymentProvider> _paymentProviderMock;
        private IPaymentService _paymentService;

        [TestInitialize()]
        public void TestInitialize()
        {
            _paymentSettings = new PaymentSettings();
            _paymentSettings.ActivePaymentProviderSystemNames = new List<string>();
            _paymentSettings.ActivePaymentProviderSystemNames.Add("Payments.TestMethod");
            _settingService = new Mock<ISettingService>();
            _paymentProviderMock = new Mock<IPaymentProvider>();
            _paymentService = new PaymentService(_paymentSettings, new List<IPaymentProvider>() { _paymentProviderMock.Object }, _settingService.Object);
        }

        [TestMethod()]
        public void LoadPaymentMethodBySystemName_ReturnPeyment()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var systemName = "systemName";
            var result = _paymentService.LoadPaymentMethodBySystemName(systemName);
            Assert.AreEqual(result, _paymentProviderMock.Object);
        }

        [TestMethod()]
        public void GetRestrictedCountryIds()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var expectedResult = new List<string>() { "1", "2", "3", "4" };
            var expectedKey = "PaymentMethodRestictions.systemName";
            _settingService.Setup(s => s.GetSettingByKey<PaymentRestictedSettings>(It.IsAny<string>(), null, ""))
                .Returns(() => new PaymentRestictedSettings() { Ids = expectedResult });

            var result = _paymentService.GetRestrictedCountryIds(_paymentProviderMock.Object);
            Assert.IsTrue(expectedResult.SequenceEqual(result));
            _settingService.Verify(s => s.GetSettingByKey<PaymentRestictedSettings>(expectedKey, null, ""), Times.Once);
        }

        [TestMethod()]
        public void GetRestrictedCountryIds_ReturnEmptyList()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var expectedKey = "PaymentMethodRestictions.systemName";
            _settingService.Setup(s => s.GetSettingByKey<PaymentRestictedSettings>(It.IsAny<string>(), null, ""))
                .Returns(() => null);

            var result = _paymentService.GetRestrictedCountryIds(_paymentProviderMock.Object);
            Assert.IsTrue(result.Count == 0);
            _settingService.Verify(s => s.GetSettingByKey<PaymentRestictedSettings>(expectedKey, null, ""), Times.Once);
        }

        [TestMethod()]
        public async Task SaveRestictedCountryIds_InvokeSettingsService()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var countryIds = new List<string>() { "1", "2", "3", "4" };
            var expectedKey = "PaymentMethodRestictions.systemName";

            await _paymentService.SaveRestictedCountryIds(_paymentProviderMock.Object, countryIds);
            _settingService.Verify(s => s.SetSetting(expectedKey, It.IsAny<PaymentRestictedSettings>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public async Task ProcessPayment_OrderTotalZero_ReturnPaidPaymentStatus()
        {
            var request = new PaymentTransaction();
            request.TransactionAmount = 0;
            var response = await _paymentService.ProcessPayment(request);
            Assert.IsTrue(response.NewPaymentTransactionStatus == TransactionStatus.Paid);
        }

        [TestMethod()]
        public async Task ProcessPayment_InvokeProcessPaymentFromPaymentMethod()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var request = new PaymentTransaction() { PaymentMethodSystemName = "systemName", TransactionAmount = 500 };


            await _paymentService.ProcessPayment(request);
            _paymentProviderMock.Verify(m => m.ProcessPayment(request), Times.Once);
        }

        [TestMethod()]
        public void ProcessPayment_NotFoundPaymentMethod_ThrowException()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName2");
            var request = new PaymentTransaction() { PaymentMethodSystemName = "systemName", TransactionAmount = 500 };
            Assert.ThrowsExceptionAsync<GrandException>(async () => await _paymentService.ProcessPayment(request));
        }

        [TestMethod()]
        public async Task PostProcessPayment_InvokePostProccessFromPaymentMethod()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var request = new PaymentTransaction() { PaymentMethodSystemName = "systemName", TransactionAmount = 500, TransactionStatus = TransactionStatus.Authorized };
            await _paymentService.PostProcessPayment(request);
            _paymentProviderMock.Verify(m => m.PostProcessPayment(request), Times.Once);
        }

        [TestMethod()]
        public void PostProcessPayment_NotFoundPaymentMethod_ThrowException()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            var request = new PaymentTransaction() { PaymentMethodSystemName = "systemName2", TransactionAmount = 500, TransactionStatus = TransactionStatus.Authorized };
            Assert.ThrowsExceptionAsync<GrandException>(async () => await _paymentService.PostProcessPayment(request), "Payment method couldn't be loaded");
        }

        [TestMethod]
        public async Task CanRePostRedirectPayment_ReturnExpectedResult()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            _paymentProviderMock.Setup(c => c.CanRePostRedirectPayment(It.IsAny<PaymentTransaction>())).ReturnsAsync(true);
            var request = new PaymentTransaction() { PaymentMethodSystemName = "systemName" };
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
            _paymentProviderMock.Setup(c => c.GetAdditionalHandlingFee(It.IsAny<IList<ShoppingCartItem>>())).ReturnsAsync(100);
            var cart = new List<ShoppingCartItem>()
            {
                new ShoppingCartItem(),
                new ShoppingCartItem()
            };
            Assert.AreEqual(100,await _paymentService.GetAdditionalHandlingFee(cart, "systemName"));
        }

        [TestMethod]
        public async Task GetAdditionalHandlingFee_SystemnameNull_ReturnZero()
        {
            _paymentProviderMock.Setup(c => c.SystemName).Returns("systemName");
            _paymentProviderMock.Setup(c => c.GetAdditionalHandlingFee(It.IsAny<IList<ShoppingCartItem>>())).ReturnsAsync(100);
            var cart = new List<ShoppingCartItem>()
            {
                new ShoppingCartItem(),
                new ShoppingCartItem()
            };
            Assert.AreEqual(0, await _paymentService.GetAdditionalHandlingFee(cart, null));
        }
    }
}
