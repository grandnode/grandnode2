using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Service.Prices
{
    [TestClass()]
    public class PriceServiceTests
    {
        private Store _store;
        private Currency _currency;
        private CurrencySettings _currencySettings;
        private Mock<IWorkContext> tempWorkContext;
        private IWorkContext _workContext;
        private Mock<IDiscountService> tempDiscountServiceMock;
        private IDiscountService _discountService;
        private ICategoryService _categoryService;
        private IBrandService _brandService;
        private ICollectionService _collectionService;
        private IProductAttributeParser _productAttributeParser;
        private Mock<IProductService> tempProductService;
        private IProductService _productService;
        private ShoppingCartSettings _shoppingCartSettings;
        private CatalogSettings _catalogSettings;
        private IPricingService _pricingService;
        private ICurrencyService _currencyService;
        private IMediator _eventPublisher;


        [TestInitialize()]
        public void TestInitialize()
        {
            CommonPath.BaseDirectory = "";

            _store = new Store { Id = "1" };
            tempWorkContext = new Mock<IWorkContext>();
            {
                tempWorkContext.Setup(instance => instance.WorkingCurrency).Returns(_currency);
                tempWorkContext.Setup(c => c.CurrentStore).Returns(_store);
                _workContext = tempWorkContext.Object;
            }

            tempDiscountServiceMock = new Mock<IDiscountService>();
            {
                _discountService = tempDiscountServiceMock.Object;
            }

            _categoryService = new Mock<ICategoryService>().Object;
            _collectionService = new Mock<ICollectionService>().Object;
            _brandService = new Mock<IBrandService>().Object;

            tempProductService = new Mock<IProductService>();
            {
                _productService = tempProductService.Object;
            }
            _productAttributeParser = new Mock<IProductAttributeParser>().Object;
            _shoppingCartSettings = new ShoppingCartSettings();
            _catalogSettings = new CatalogSettings();
            var eventPublisher = new Mock<IMediator>();
            _eventPublisher = eventPublisher.Object;

            _currencySettings = new CurrencySettings();
            _currencySettings.PrimaryExchangeRateCurrencyId = "1";
            _currencySettings.PrimaryStoreCurrencyId = "1";
            //_currencyService = new Mock<ICurrencyService>().Object;
            _currency = new Currency { Id = "1", CurrencyCode = "USD", Rate = 1, Published = true, MidpointRoundId = System.MidpointRounding.ToEven, RoundingTypeId = RoundingType.Rounding001 };

            var tempCurrency = new Mock<ICurrencyService>();
            {
                tempCurrency.Setup(instance => instance.GetPrimaryStoreCurrency()).Returns(Task.FromResult(_currency));
                //tempCurrency.Setup(instance => instance.ConvertToPrimaryStoreCurrency(It.IsAny<double>(), _currency)).ReturnsAsync(5);
                //_currencyService = tempCurrency.Object;
            }
            var cacheManager = new Mock<ICacheBase>().Object;
            IRepository<Currency> _currencyRepository;
            var tempCurrencyRepository = new Mock<IRepository<Currency>>();
            {
                var IMongoCollection = new Mock<MongoRepository<Currency>>().Object;
                IMongoCollection.Insert(_currency);


                tempCurrencyRepository.Setup(x => x.Table).Returns(IMongoCollection.Table);
                tempCurrencyRepository.Setup(x => x.GetByIdAsync(_currency.Id)).ReturnsAsync(_currency);
                _currencyRepository = tempCurrencyRepository.Object;
            }

            IAclService _aclService = new Mock<IAclService>().Object;
            var _serviceProvider = new Mock<IServiceProvider>().Object;

            _currencyService = new CurrencyService(
                cacheManager, _currencyRepository, _aclService,
                _currencySettings, _eventPublisher);


            _pricingService = new PricingService(
                _workContext,
                _discountService,
                _categoryService,
                _brandService,
                _collectionService,
                _productAttributeParser,
                _productService,
                _eventPublisher,
                _currencyService,
                _shoppingCartSettings,
                _catalogSettings);
        }

        [TestMethod()]
        public async Task Can_get_final_product_price()
        {
            var product = new Product {
                Id = "1",
                Name = "product name 01",
                Price = 49.99,
                EnteredPrice = false,
                Published = true,
            };
            product.ProductPrices.Add(new ProductPrice() { CurrencyCode = "USD", Price = 49.99 });

            var currency = new Currency { Id = "1", CurrencyCode = "USD", Rate = 1, Published = true, MidpointRoundId = System.MidpointRounding.ToEven, RoundingTypeId = RoundingType.Rounding001 };
            var customer = new Customer();
            var pr = (await _pricingService.GetFinalPrice(product, customer, currency, 0, false, 1));
            Assert.AreEqual(49.99, pr.finalPrice);
            //returned price FOR ONE UNIT should be the same, even if quantity is different than 1
            Assert.AreEqual(49.99, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 10)).finalPrice);
        }

        [TestMethod()]
        public async Task Can_get_final_product_price_with_tier_prices()
        {
            var product = new Product {
                Id = "1",
                Name = "product name 01",
                Price = 49.99,
                EnteredPrice = false,
                Published = true,
            };
            product.ProductPrices.Add(new ProductPrice() { CurrencyCode = "USD", Price = 49.99 });
            //TierPrice is simply "the more you buy, the less you pay"
            product.TierPrices.Add(new TierPrice { Price = 10, Quantity = 10, CurrencyCode = "USD" });
            product.TierPrices.Add(new TierPrice { Price = 2, Quantity = 200, CurrencyCode = "USD" });

            Customer customer = new Customer();

            /*
            quantity: <=9           price: 49.99
            quantity: 10-199        price: 10
            quantity: >=200         price: 2
            */

            Assert.AreEqual(49.99, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 1)).finalPrice);
            Assert.AreEqual(49.99, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 5)).finalPrice);
            Assert.AreEqual(49.99, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 9)).finalPrice);

            Assert.AreEqual(10, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 10)).finalPrice);
            Assert.AreEqual(10, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 11)).finalPrice);
            Assert.AreEqual(10, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 151)).finalPrice);
            Assert.AreEqual(10, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 199)).finalPrice);

            var p1 = (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 200)).finalPrice;
            Assert.AreEqual(2, p1);
            Assert.AreEqual(2, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 201)).finalPrice);
            Assert.AreEqual(2, (await _pricingService.GetFinalPrice(product, customer, _currency, 0, false, 22201)).finalPrice);
        }


        [TestMethod()]
        public async Task Can_get_final_product_price_with_additionalFee()
        {
            //tests if price is valid for additional charge (additional fee) 
            var product = new Product {
                Id = "1",
                Name = "product name 01",
                Price = 49.99,
                EnteredPrice = false,
                Published = true
            };
            product.ProductPrices.Add(new ProductPrice() { CurrencyCode = "USD", Price = 49.99 });
            var customer = new Customer();

            //additional charge +1000
            //==1049.99
            Assert.AreEqual(1049.99, (await _pricingService.GetFinalPrice(product, customer, _currency, 1000, false, 1)).finalPrice);
        }

        [TestMethod()]
        public async Task Can_get_final_product_price_with_discount()
        {
            var product = new Product {
                Id = "1",
                Name = "product name 01",
                Price = 49.99,
                EnteredPrice = false,
                Published = true,
            };
            product.ProductPrices.Add(new ProductPrice() { CurrencyCode = "USD", Price = 49.99 });
            var customer = new Customer();

            var discount001 = new Discount {
                Id = "1",
                Name = "Discount 001",
                DiscountTypeId = DiscountType.AssignedToSkus,
                DiscountAmount = 10,
                DiscountLimitationId = DiscountLimitationType.Nolimits,
                CurrencyCode = _currency.CurrencyCode
            };

            tempDiscountServiceMock.Setup(x => x.GetDiscountById(discount001.Id)).ReturnsAsync(discount001);

            product.AppliedDiscounts.Add(discount001.Id);

            tempDiscountServiceMock.Setup(x => x.ValidateDiscount(discount001, customer, _currency)).ReturnsAsync(new DiscountValidationResult() { IsValid = true });
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCategories, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCollections, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToAllProducts, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToSkus, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>() { discount001 });

            var discountAmount = discount001.DiscountAmount;
            tempDiscountServiceMock.Setup(x => x.GetPreferredDiscount(It.IsAny<List<ApplyDiscount>>(), customer, _currency, product, 49.99)).ReturnsAsync((new List<ApplyDiscount>(), 10));

            //it should return 39.99 - price cheaper about 10 
            var finalprice = await _pricingService.GetFinalPrice(product, customer, _currency, 0, true, 1);
            var pp = finalprice.finalPrice;

            Assert.AreEqual(39.99, pp);
        }

        [TestMethod()]
        public async Task Can_get_shopping_cart_item_unitPrice()
        {
            var customer001 = new Customer { Id = "98767" };
            tempWorkContext.Setup(x => x.CurrentCustomer).Returns(customer001);
            tempWorkContext.Setup(x => x.WorkingCurrency).Returns(_currency);
            var product001 = new Product {
                Id = "242422",
                Name = "product name 01",
                Price = 49.99,
                EnteredPrice = false,
                Published = true,
            };
            product001.ProductPrices.Add(new ProductPrice() { CurrencyCode = "USD", Price = 49.99 });
            tempProductService.Setup(x => x.GetProductById("242422", false)).ReturnsAsync(product001);


            var shoppingCartItem = new ShoppingCartItem {
                ProductId = product001.Id,
                Quantity = 2
            };

            customer001.ShoppingCartItems.Add(shoppingCartItem);

            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCategories, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCollections, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToAllProducts, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            var pp = (await _pricingService.GetUnitPrice(shoppingCartItem, product001)).unitprice;
            Assert.AreEqual(49.99, pp);
        }

        [TestMethod()]
        public async Task Can_get_shopping_cart_item_subTotal()
        {
            var product001 = new Product {
                Id = "242422",
                Name = "product name 01",
                Price = 55.11,
                EnteredPrice = false,
                Published = true,
            };
            product001.ProductPrices.Add(new ProductPrice() { CurrencyCode = "USD", Price = 55.11 });
            tempProductService.Setup(x => x.GetProductById("242422", false)).ReturnsAsync(product001);

            var customer001 = new Customer { Id = "98767" };
            tempWorkContext.Setup(x => x.CurrentCustomer).Returns(customer001);
            tempWorkContext.Setup(x => x.WorkingCurrency).Returns(_currency);
            var shoppingCartItem = new ShoppingCartItem {
                ProductId = product001.Id, //222
                Quantity = 2
            };

            customer001.ShoppingCartItems.Add(shoppingCartItem);

            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCategories, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCollections, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToAllProducts, "1", _currency.CurrencyCode, "", "", false)).ReturnsAsync(new List<Discount>());
            var subtotal = (await _pricingService.GetSubTotal(shoppingCartItem, product001)).subTotal;
            Assert.AreEqual(110.22, subtotal);
        }
    }
}
