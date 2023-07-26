using Grand.Business.Checkout.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Customers
{
    [TestClass()]
    public class CustomerRegisteredEventHandlerTests
    {
        CustomerRegisteredEventHandler _customerRegisteredEventHandler;
        private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
        private Domain.Orders.LoyaltyPointsSettings _loyaltyPointsSettings;
        [TestInitialize()]
        public void Init()
        {
            var translationServiceMock = new Mock<ITranslationService>();
            translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns("Name");
            _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
            _loyaltyPointsSettings = new Domain.Orders.LoyaltyPointsSettings() { Enabled = true, PointsForRegistration = 10 };
            _customerRegisteredEventHandler = new CustomerRegisteredEventHandler(translationServiceMock.Object, _loyaltyPointsServiceMock.Object, _loyaltyPointsSettings);
        }

        [TestMethod()]
        public async Task HandleTest_LoyaltyPointsSettings_Enabled()
        {
            //Act
            await _customerRegisteredEventHandler.Handle(new Core.Events.Customers.CustomerRegisteredEvent(new Domain.Customers.Customer()), CancellationToken.None);
            //Assert
            _loyaltyPointsServiceMock.Verify(c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);
        }
        [TestMethod()]
        public async Task HandleTest_LoyaltyPointsSettings_Disabled()
        {
            _loyaltyPointsSettings.Enabled = false;
            //Act
            await _customerRegisteredEventHandler.Handle(new Core.Events.Customers.CustomerRegisteredEvent(new Domain.Customers.Customer()), CancellationToken.None);
            //Assert
            _loyaltyPointsServiceMock.Verify(c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Never);
        }
    }
}