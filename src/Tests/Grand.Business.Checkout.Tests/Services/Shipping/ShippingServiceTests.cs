using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Services.Shipping;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Services.Shipping
{
    [TestClass]
    public class ShippingServiceTests
    {
        private Mock<IWarehouseService> _warehouseMock;
        private Mock<ILogger> _loggerMock;
        private Mock<ITranslationService> _translationServiceMock;
        private Mock<ICountryService> _countryServiceMokc;
        private ShippingProviderSettings _shippingProviderSettings;
        private ShippingSettings _shippingSettings;
        private Mock<IShippingRateCalculationProvider> _rateProviderMock;
        private IShippingService _service;

        [TestInitialize]
        public void Init()
        {
            _warehouseMock = new Mock<IWarehouseService>();
            _loggerMock = new Mock<ILogger>();
            _translationServiceMock = new Mock<ITranslationService>();
            _countryServiceMokc = new Mock<ICountryService>();
            _shippingProviderSettings = new ShippingProviderSettings();
            _shippingSettings = new ShippingSettings();
            _rateProviderMock = new Mock<IShippingRateCalculationProvider>();
            _service = new ShippingService(_warehouseMock.Object,_loggerMock.Object,_translationServiceMock.Object,_countryServiceMokc.Object,
                new List<IShippingRateCalculationProvider>() { _rateProviderMock.Object},_shippingProviderSettings,_shippingSettings);
        }

        [TestMethod]
        public async Task LoadActiveShippingRateCalculationProviders_IsLimitToStore_ReturnEmptyList()
        {
            _rateProviderMock.Setup(c => c.LimitedToStores).Returns(new List<string>());
            _rateProviderMock.Setup(c => c.LimitedToGroups).Returns(new List<string>());
            var result = await _service.LoadActiveShippingRateCalculationProviders(new Customer(),"storeId");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task LoadActiveShippingRateCalculationProviders_HideShipmentMethods_ReturnEmptyList()
        {
            _rateProviderMock.Setup(c => c.LimitedToStores).Returns(new List<string>() { "storeId"});
            _rateProviderMock.Setup(c => c.LimitedToGroups).Returns(new List<string>() {  });
            _rateProviderMock.Setup(c => c.HideShipmentMethods(It.IsAny<IList<ShoppingCartItem>>())).ReturnsAsync(true);
            var result = await _service.LoadActiveShippingRateCalculationProviders(new Customer(), "storeId");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task LoadActiveShippingRateCalculationProviders_ReturnExpectedValues()
        {
            _shippingProviderSettings.ActiveSystemNames = new List<string>() { "sysname" };
            _rateProviderMock.Setup(c => c.LimitedToStores).Returns(new List<string>() { "storeId" });
            _rateProviderMock.Setup(c => c.LimitedToGroups).Returns(new List<string>() {  });
            _rateProviderMock.Setup(c => c.SystemName).Returns("sysname");
            _rateProviderMock.Setup(c => c.HideShipmentMethods(It.IsAny<IList<ShoppingCartItem>>())).ReturnsAsync(false);
            var result = await _service.LoadActiveShippingRateCalculationProviders(new Customer(), "storeId");
            Assert.IsTrue(result.Count == 1);
            Assert.AreEqual(result.First().SystemName,_rateProviderMock.Object.SystemName);
        }

        [TestMethod]
        public async Task CreateShippingOptionRequests_ReturnExpectedResults()
        {
            var cart = new List<ShoppingCartItem>
            {
                new ShoppingCartItem()
                {
                    IsShipEnabled=true,
                    WarehouseId="id"
                }
            };

            var customer = new Customer();
            var shippingAddress = new Address();
            var store = new Store() { Id = "id" };
            var warehouse = new Warehouse()
            {
                Address =null
            };
            _warehouseMock.Setup(c => c.GetWarehouseById(It.IsAny<string>())).ReturnsAsync(warehouse);

            var result = await _service.CreateShippingOptionRequests(customer, cart, shippingAddress, store);

            Assert.AreEqual(result.ShippingAddress, shippingAddress);            
            Assert.AreEqual(result.StoreId, "id");
            Assert.AreEqual(result.Customer, customer);
        }

        [TestMethod]
        public async Task CreateShippingOptionRequests_ShipNotEnable_ReturnEmptyList()
        {
            var cart = new List<ShoppingCartItem>
            {
                new ShoppingCartItem()
                {
                    IsShipEnabled=false,
                    WarehouseId="id"
                }
            };

            var customer = new Customer();
            var shippingAddress = new Address();
            var store = new Store() { Id = "id" };
            var warehouse = new Warehouse()
            {
                Address = null
            };
            _warehouseMock.Setup(c => c.GetWarehouseById(It.IsAny<string>())).ReturnsAsync(warehouse);

            var result = await _service.CreateShippingOptionRequests(customer, cart, shippingAddress, store);
            Assert.IsTrue(result != null);
        }
    }
}
