using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Common.Services.Directory;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Mongo;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
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

namespace Grand.Business.Catalog.Tests.Services.Prices;

[TestClass]
public class PriceServiceTests
{
    private CatalogSettings _catalogSettings;
    private Currency _currency;
    private ICurrencyService _currencyService;
    private CurrencySettings _currencySettings;
    private IMediator _eventPublisher;
    private IPricingService _pricingService;
    private IProductService _productService;
    private ShoppingCartSettings _shoppingCartSettings;
    private Store _store;
    private IContextAccessor _workContext;
    private Mock<IDiscountService> tempDiscountServiceMock;
    private Mock<IProductService> tempProductService;
    private Mock<IContextAccessor> tempWorkContext;
    private Mock<IDiscountHandlerService> tempDiscountApplicationService;
    private IDiscountService _discountService;
    [TestInitialize]
    public void TestInitialize()
    {
        var settingsPath = Path.Combine("", CommonPath.AppData, CommonPath.SettingsFile);
        DataSettingsManager.Initialize(settingsPath);

        _store = new Store { Id = "1" };
        tempWorkContext = new Mock<IContextAccessor>();
        {
            tempWorkContext.Setup(instance => instance.WorkContext.WorkingCurrency).Returns(_currency);
            tempWorkContext.Setup(c => c.StoreContext.CurrentStore).Returns(_store);
            _workContext = tempWorkContext.Object;
        }

        tempProductService = new Mock<IProductService>();
        {
            _productService = tempProductService.Object;
        }
        _shoppingCartSettings = new ShoppingCartSettings();
        _catalogSettings = new CatalogSettings();
        var eventPublisher = new Mock<IMediator>();
        _eventPublisher = eventPublisher.Object;

        _currencySettings = new CurrencySettings {
            PrimaryExchangeRateCurrencyId = "1",
            PrimaryStoreCurrencyId = "1"
        };

        _currency = new Currency {
            Id = "1",
            CurrencyCode = "USD",
            Rate = 1,
            Published = true,
            MidpointRoundId = MidpointRounding.ToEven,
            RoundingTypeId = RoundingType.Rounding001
        };

        tempDiscountServiceMock = new Mock<IDiscountService>();
        {
            _discountService = tempDiscountServiceMock.Object;
        }
        var tempCurrency = new Mock<ICurrencyService>();
        {
            tempCurrency.Setup(instance => instance.GetPrimaryStoreCurrency()).Returns(Task.FromResult(_currency));
        }
        var cacheManager = new Mock<ICacheBase>().Object;
        IRepository<Currency> _currencyRepository;
        var tempCurrencyRepository = new Mock<IRepository<Currency>>();
        {
            var IMongoCollection = new Mock<MongoRepository<Currency>>(Mock.Of<IAuditInfoProvider>()).Object;
            IMongoCollection.Insert(_currency);


            tempCurrencyRepository.Setup(x => x.Table).Returns(IMongoCollection.Table);
            tempCurrencyRepository.Setup(x => x.GetByIdAsync(_currency.Id)).ReturnsAsync(_currency);
            _currencyRepository = tempCurrencyRepository.Object;
        }

        var _aclService = new Mock<IAclService>().Object;
        var _serviceProvider = new Mock<IServiceProvider>().Object;

        _currencyService = new CurrencyService(
            cacheManager, _currencyRepository, _aclService,
            _currencySettings, _eventPublisher);

        tempDiscountApplicationService = new Mock<IDiscountHandlerService>();

        _pricingService = new PricingService(
            _workContext,
            _productService,
            _eventPublisher,
            _currencyService,
            tempDiscountApplicationService.Object,
            _shoppingCartSettings,
            _catalogSettings);
    }

    [TestMethod]
    public async Task Can_get_final_product_price()
    {
        var product = new Product {
            Id = "1",
            Name = "product name 01",
            Price = 49.99,
            EnteredPrice = false,
            Published = true
        };
        product.ProductPrices.Add(new ProductPrice { CurrencyCode = "USD", Price = 49.99 });

        var currency = new Currency {
            Id = "1",
            CurrencyCode = "USD",
            Rate = 1,
            Published = true,
            MidpointRoundId = MidpointRounding.ToEven,
            RoundingTypeId = RoundingType.Rounding001
        };
        var customer = new Customer();
        var pr = await _pricingService.GetFinalPrice(product, customer, _store, currency, 0, false);
        Assert.AreEqual(49.99, pr.finalPrice);
        //returned price FOR ONE UNIT should be the same, even if quantity is different than 1
        Assert.AreEqual(49.99,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 10)).finalPrice);
    }

    [TestMethod]
    public async Task Can_get_final_product_price_with_tier_prices()
    {
        var product = new Product {
            Id = "1",
            Name = "product name 01",
            Price = 49.99,
            EnteredPrice = false,
            Published = true
        };
        product.ProductPrices.Add(new ProductPrice { CurrencyCode = "USD", Price = 49.99 });
        //TierPrice is simply "the more you buy, the less you pay"
        product.TierPrices.Add(new TierPrice { Price = 10, Quantity = 10, CurrencyCode = "USD" });
        product.TierPrices.Add(new TierPrice { Price = 2, Quantity = 200, CurrencyCode = "USD" });

        var customer = new Customer();

        /*
        quantity: <=9           price: 49.99
        quantity: 10-199        price: 10
        quantity: >=200         price: 2
        */

        Assert.AreEqual(49.99,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false)).finalPrice);
        Assert.AreEqual(49.99,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 5)).finalPrice);
        Assert.AreEqual(49.99,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 9)).finalPrice);

        Assert.AreEqual(10,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 10)).finalPrice);
        Assert.AreEqual(10,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 11)).finalPrice);
        Assert.AreEqual(10,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 151)).finalPrice);
        Assert.AreEqual(10,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 199)).finalPrice);

        var p1 = (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 200)).finalPrice;
        Assert.AreEqual(2, p1);
        Assert.AreEqual(2,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 201)).finalPrice);
        Assert.AreEqual(2,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 0, false, 22201)).finalPrice);
    }


    [TestMethod]
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
        product.ProductPrices.Add(new ProductPrice { CurrencyCode = "USD", Price = 49.99 });
        var customer = new Customer();

        //additional charge +1000
        //==1049.99
        Assert.AreEqual(1049.99,
            (await _pricingService.GetFinalPrice(product, customer, _store, _currency, 1000, false)).finalPrice);
    }

    [TestMethod]
    public async Task Can_get_final_product_price_with_discount()
    {
        var product = new Product {
            Id = "1",
            Name = "product name 01",
            Price = 49.99,
            EnteredPrice = false,
            Published = true
        };
        product.ProductPrices.Add(new ProductPrice { CurrencyCode = "USD", Price = 49.99 });
        var customer = new Customer();

        var discount001 = new Discount {
            Id = "1",
            Name = "Discount 001",
            DiscountTypeId = DiscountType.AssignedToSkus,
            DiscountAmount = 10,
            DiscountLimitationId = DiscountLimitationType.Nolimits,
            CurrencyCode = _currency.CurrencyCode
        };

        product.AppliedDiscounts.Add(discount001.Id);

        var discountAmount = discount001.DiscountAmount;
        tempDiscountApplicationService.Setup(x => x.GetAllowedDiscounts(It.IsAny<Product>(), It.IsAny<Customer>(), It.IsAny<Store>(),
            It.IsAny<Currency>())).ReturnsAsync((new List<ApplyDiscount>() {
                     new ApplyDiscount()
                        {
                        DiscountId = discount001.Id
                    }
            }));

        tempDiscountApplicationService.Setup(x => x.GetDiscountAmount(It.IsAny<Product>(), It.IsAny<Customer>(), It.IsAny<Store>(), It.IsAny<Currency>(), It.IsAny<double>()))
            .ReturnsAsync((new List<ApplyDiscount>(), 10));

        //it should return 39.99 - price cheaper about 10 
        var finalprice = await _pricingService.GetFinalPrice(product, customer, _store, _currency);
        var pp = finalprice.finalPrice;

        Assert.AreEqual(39.99, pp);
    }

    [TestMethod]
    public async Task Can_get_shopping_cart_item_unitPrice()
    {
        var customer001 = new Customer { Id = "98767" };
        tempWorkContext.Setup(x => x.WorkContext.CurrentCustomer).Returns(customer001);
        tempWorkContext.Setup(x => x.WorkContext.WorkingCurrency).Returns(_currency);
        var product001 = new Product {
            Id = "242422",
            Name = "product name 01",
            Price = 49.99,
            EnteredPrice = false,
            Published = true
        };
        product001.ProductPrices.Add(new ProductPrice { CurrencyCode = "USD", Price = 49.99 });
        tempProductService.Setup(x => x.GetProductById("242422", false)).ReturnsAsync(product001);


        var shoppingCartItem = new ShoppingCartItem {
            ProductId = product001.Id,
            Quantity = 2
        };

        customer001.ShoppingCartItems.Add(shoppingCartItem);

        tempDiscountServiceMock
            .Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToCategories, "1", _currency.CurrencyCode))
            .ReturnsAsync(new List<Discount>());
        tempDiscountServiceMock
            .Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToCollections, "1", _currency.CurrencyCode))
            .ReturnsAsync(new List<Discount>());
        tempDiscountServiceMock
            .Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToAllProducts, "1", _currency.CurrencyCode))
            .ReturnsAsync(new List<Discount>());
        var pp = (await _pricingService.GetUnitPrice(shoppingCartItem, product001)).unitprice;
        Assert.AreEqual(49.99, pp);
    }

    [TestMethod]
    public async Task Can_get_shopping_cart_item_subTotal()
    {
        var product001 = new Product {
            Id = "242422",
            Name = "product name 01",
            Price = 55.11,
            EnteredPrice = false,
            Published = true
        };
        product001.ProductPrices.Add(new ProductPrice { CurrencyCode = "USD", Price = 55.11 });
        tempProductService.Setup(x => x.GetProductById("242422", false)).ReturnsAsync(product001);

        var customer001 = new Customer { Id = "98767" };
        tempWorkContext.Setup(x => x.WorkContext.CurrentCustomer).Returns(customer001);
        tempWorkContext.Setup(x => x.WorkContext.WorkingCurrency).Returns(_currency);
        var shoppingCartItem = new ShoppingCartItem {
            ProductId = product001.Id, //222
            Quantity = 2
        };

        customer001.ShoppingCartItems.Add(shoppingCartItem);

        tempDiscountServiceMock
            .Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToCategories, "1", _currency.CurrencyCode))
            .ReturnsAsync(new List<Discount>());
        tempDiscountServiceMock
            .Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToCollections, "1", _currency.CurrencyCode))
            .ReturnsAsync(new List<Discount>());
        tempDiscountServiceMock
            .Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToAllProducts, "1", _currency.CurrencyCode))
            .ReturnsAsync(new List<Discount>());
        var subtotal = (await _pricingService.GetSubTotal(shoppingCartItem, product001)).subTotal;
        Assert.AreEqual(110.22, subtotal);
    }

    [TestMethod]
    public async Task Can_get_product_cost()
    {
        var product001 = new Product {
            Id = "242422",
            Name = "product name 01",
            Price = 55.11,
            EnteredPrice = false,
            Published = true,
            ProductCost = 20
        };

        var productCost = await _pricingService.GetProductCost(product001, new List<CustomAttribute>());

        Assert.AreEqual(20, productCost);
    }
}