using Grand.Business.Checkout.Services.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Vendors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Shipping;

[TestClass]
public class ReturnShippingServiceTests
{
    private const string StoreId = "store";

    private List<MerchandiseReturnAction> _actions;
    private Mock<ICustomerService> _customerService;
    private Mock<IMerchandiseReturnService> _merchandiseReturnService;
    private Order _order;
    private OrderSettings _orderSettings;
    private Mock<IReturnShippingProvider> _provider;
    private ReturnShippingProviderSettings _providerSettings;
    private List<MerchandiseReturnReason> _reasons;
    private ReturnShippingService _service;
    private Mock<ISettingService> _settingService;
    private ShippingSettings _shippingSettings;
    private Mock<IVendorService> _vendorService;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1", StoreId = StoreId, CurrencyRate = 1.5 };
        _order.OrderItems.Add(new OrderItem { Id = "a", Quantity = 5 });
        _order.OrderItems.Add(new OrderItem { Id = "b", Quantity = 5, VendorId = "v1" });

        _providerSettings = new ReturnShippingProviderSettings { ActiveSystemNames = ["Return.Test"] };
        _orderSettings = new OrderSettings { MerchandiseReturnFees = MerchandiseReturnFees.CustomerPays };
        _shippingSettings = new ShippingSettings { ShippingOriginAddress = new Address { Address1 = "Origin" } };
        _settingService = new Mock<ISettingService>();
        _settingService.Setup(x => x.LoadSetting<ReturnShippingProviderSettings>(StoreId))
            .ReturnsAsync(() => _providerSettings);
        _settingService.Setup(x => x.LoadSetting<OrderSettings>(StoreId)).ReturnsAsync(() => _orderSettings);
        _settingService.Setup(x => x.LoadSetting<ShippingSettings>(StoreId)).ReturnsAsync(() => _shippingSettings);

        _reasons = [];
        _actions = [];
        _merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnReasons(StoreId)).ReturnsAsync(() => _reasons);
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnActions(StoreId)).ReturnsAsync(() => _actions);

        _vendorService = new Mock<IVendorService>();
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1" });

        _provider = Provider("Return.Test");
        _service = Service(_provider);
    }

    private ReturnShippingService Service(params Mock<IReturnShippingProvider>[] providers)
    {
        return new ReturnShippingService(providers.Select(x => x.Object), _settingService.Object,
            _merchandiseReturnService.Object, _vendorService.Object, _customerService.Object);
    }

    private static Mock<IReturnShippingProvider> Provider(string systemName, IList<string> limitedToStores = null)
    {
        var provider = new Mock<IReturnShippingProvider>();
        provider.Setup(x => x.SystemName).Returns(systemName);
        provider.Setup(x => x.LimitedToStores).Returns(limitedToStores ?? []);
        provider.Setup(x => x.LimitedToGroups).Returns([]);
        provider.Setup(x => x.GetReturnOptions(It.IsAny<ReturnShippingRequest>())).ReturnsAsync([]);
        return provider;
    }

    private void SetupOptions(string vendorId, params ReturnShippingOption[] options)
    {
        _provider.Setup(x => x.GetReturnOptions(It.Is<ReturnShippingRequest>(r => r.VendorId == vendorId)))
            .ReturnsAsync(options);
    }

    private static ReturnShippingOption Option(string code, double cost, bool hasForm = false,
        bool createsLabel = false)
    {
        return new ReturnShippingOption
            { Code = code, Name = "Option " + code, Cost = cost, HasForm = hasForm, CreatesLabel = createsLabel };
    }

    private MerchandiseReturnSubmitRequest Request(params MerchandiseReturnSubmitItem[] items)
    {
        return new MerchandiseReturnSubmitRequest
            { Order = _order, StoreId = StoreId, CustomerId = "c1", LanguageId = "l1", Items = items };
    }

    private static MerchandiseReturnSubmitItem Item(string orderItemId, string reasonId = null,
        string actionId = null)
    {
        return new MerchandiseReturnSubmitItem(orderItemId, 1, reasonId, actionId);
    }

    [TestMethod]
    public async Task LoadActiveProviders_NotInActiveList_Excluded()
    {
        var other = Provider("Return.Other");
        var service = Service(_provider, other);

        var providers = await service.LoadActiveProviders(new Customer(), StoreId);

        Assert.HasCount(1, providers);
        Assert.AreEqual("Return.Test", providers[0].SystemName);
    }

    [TestMethod]
    public async Task LoadActiveProviders_LimitedToOtherStore_Excluded()
    {
        var limited = Provider("Return.Test", ["other-store"]);
        var service = Service(limited);

        var providers = await service.LoadActiveProviders(new Customer(), StoreId);

        Assert.IsEmpty(providers);
    }

    [TestMethod]
    public async Task GetOffers_NoActiveProvider_Empty()
    {
        _providerSettings.ActiveSystemNames = [];
        SetupOptions("", Option("a", 5));

        var offers = await _service.GetOffers(Request(Item("a")));

        Assert.IsEmpty(offers);
    }

    [TestMethod]
    public async Task GetOffers_NoItemSubmitted_Empty()
    {
        SetupOptions("", Option("a", 5));

        var offers = await _service.GetOffers(Request(new MerchandiseReturnSubmitItem("a", 0, null, null)));

        Assert.IsEmpty(offers);
    }

    [TestMethod]
    public async Task GetOffers_OptionMissingForOneVendor_IsHidden()
    {
        SetupOptions("", Option("a", 5));
        SetupOptions("v1");

        var offers = await _service.GetOffers(Request(Item("a"), Item("b")));

        Assert.IsEmpty(offers);
    }

    [TestMethod]
    public async Task GetOffers_TwoVendors_SumsCost()
    {
        SetupOptions("", Option("a", 5));
        SetupOptions("v1", Option("a", 5));

        var offers = await _service.GetOffers(Request(Item("a"), Item("b")));

        Assert.HasCount(1, offers);
        Assert.AreEqual("Return.Test", offers[0].ProviderSystemName);
        Assert.AreEqual("a", offers[0].OptionCode);
        Assert.AreEqual(10, offers[0].Cost);
        Assert.AreEqual(2, offers[0].Parcels);
    }

    [TestMethod]
    public async Task GetOffers_FreeReason_CostZero()
    {
        _reasons.Add(new MerchandiseReturnReason { Id = "damaged", FreeReturnShipping = true });
        SetupOptions("", Option("a", 5));

        var offers = await _service.GetOffers(Request(Item("a", "damaged")));

        Assert.HasCount(1, offers);
        Assert.AreEqual(0, offers[0].Cost);
    }

    [TestMethod]
    public async Task GetOffers_AllWithoutRefund_HidesPaidWhenFreeExists()
    {
        _actions.Add(new MerchandiseReturnAction { Id = "repair", WithoutRefund = true });
        SetupOptions("", Option("free", 0), Option("paid", 7));

        var offers = await _service.GetOffers(Request(Item("a", actionId: "repair")));

        Assert.HasCount(1, offers);
        Assert.AreEqual("free", offers[0].OptionCode);
        Assert.AreEqual(0, offers[0].Cost);
    }

    [TestMethod]
    public async Task GetOffers_AllWithoutRefund_NoFreeOption_PaidStaysAtZero()
    {
        _actions.Add(new MerchandiseReturnAction { Id = "repair", WithoutRefund = true });
        SetupOptions("", Option("paid", 7));

        var offers = await _service.GetOffers(Request(Item("a", actionId: "repair")));

        Assert.HasCount(1, offers);
        Assert.AreEqual("paid", offers[0].OptionCode);
        Assert.AreEqual(0, offers[0].Cost);
    }

    [TestMethod]
    public async Task GetOffers_OneItemWithRefund_KeepsPaidOptionsAndCost()
    {
        _actions.Add(new MerchandiseReturnAction { Id = "repair", WithoutRefund = true });
        SetupOptions("", Option("free", 0), Option("paid", 7));

        var offers = await _service.GetOffers(Request(Item("a", actionId: "repair"), Item("a", actionId: "unknown")));

        Assert.HasCount(2, offers);
        Assert.AreEqual(7, offers.Single(x => x.OptionCode == "paid").Cost);
    }

    [TestMethod]
    public async Task GetOffers_OptionWithForm_SetsFormRouteName()
    {
        SetupOptions("", Option("point", 3, true));
        _provider.Setup(x => x.GetFormRouteName("point")).ReturnsAsync("Plugin.Return.Point");

        var offers = await _service.GetOffers(Request(Item("a")));

        Assert.IsTrue(offers[0].HasForm);
        Assert.AreEqual("Plugin.Return.Point", offers[0].FormRouteName);
    }

    [TestMethod]
    public async Task Resolve_ProvidersActiveAndNoChoice_Error()
    {
        SetupOptions("", Option("a", 5));

        var resolution = await _service.Resolve(Request(Item("a")), null);

        CollectionAssert.AreEqual(new[] { "MerchandiseReturns.ReturnShipping.Choose" }, resolution.Errors.ToArray());
        Assert.IsEmpty(resolution.ByVendor);
    }

    [TestMethod]
    public async Task Resolve_NoProvidersAndNoChoice_NoErrors()
    {
        _providerSettings.ActiveSystemNames = [];

        var resolution = await _service.Resolve(Request(Item("a")), null);

        Assert.IsEmpty(resolution.Errors);
        Assert.IsEmpty(resolution.ByVendor);
        _merchandiseReturnService.Verify(x => x.GetAllMerchandiseReturnReasons(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Resolve_ChoiceNotOffered_Error()
    {
        SetupOptions("", Option("a", 5));

        var resolution = await _service.Resolve(Request(Item("a")),
            new ReturnShippingChoice { ProviderSystemName = "Return.Test", OptionCode = "other" });

        CollectionAssert.AreEqual(new[] { "MerchandiseReturns.ReturnShipping.Choose" }, resolution.Errors.ToArray());
        Assert.IsEmpty(resolution.ByVendor);
    }

    [TestMethod]
    public async Task Resolve_IgnoresPostedCost()
    {
        SetupOptions("", Option("point", 9, true));
        _provider.Setup(x => x.ValidateForm(It.IsAny<ReturnShippingRequest>(), "point",
                It.IsAny<IDictionary<string, string>>()))
            .ReturnsAsync(new ReturnShippingValidationResult { Cost = null });
        var choice = new ReturnShippingChoice {
            ProviderSystemName = "Return.Test", OptionCode = "point",
            Form = new Dictionary<string, string> { { "cost", "0" } }
        };

        var resolution = await _service.Resolve(Request(Item("a")), choice);

        Assert.IsEmpty(resolution.Errors);
        var shipping = resolution.ByVendor[""];
        Assert.AreEqual(9, shipping.Cost);
        Assert.AreEqual(1.5, shipping.CurrencyRate);
        Assert.AreEqual("Option point", shipping.OptionName);
        Assert.AreEqual(ReturnLabelStatus.NotRequired, shipping.LabelStatus);
        Assert.AreEqual("Origin", shipping.ReturnAddress.Address1);
    }

    [TestMethod]
    public async Task Resolve_ValidationErrors_ReturnedAndNoSnapshot()
    {
        SetupOptions("", Option("point", 9, true));
        _provider.Setup(x => x.ValidateForm(It.IsAny<ReturnShippingRequest>(), "point",
                It.IsAny<IDictionary<string, string>>()))
            .ReturnsAsync(new ReturnShippingValidationResult { Errors = ["Pick a drop-off point."] });

        var resolution = await _service.Resolve(Request(Item("a")),
            new ReturnShippingChoice { ProviderSystemName = "Return.Test", OptionCode = "point" });

        CollectionAssert.AreEqual(new[] { "Pick a drop-off point." }, resolution.Errors.ToArray());
        Assert.IsEmpty(resolution.ByVendor);
    }

    [TestMethod]
    public async Task Resolve_CreatesLabelOption_LabelStatusPending()
    {
        SetupOptions("", Option("label", 4, createsLabel: true));
        SetupOptions("v1", Option("label", 4, createsLabel: true));

        var resolution = await _service.Resolve(Request(Item("a"), Item("b")),
            new ReturnShippingChoice { ProviderSystemName = "Return.Test", OptionCode = "label" });

        Assert.IsEmpty(resolution.Errors);
        Assert.HasCount(2, resolution.ByVendor);
        Assert.AreEqual(ReturnLabelStatus.Pending, resolution.ByVendor[""].LabelStatus);
        Assert.AreEqual(ReturnLabelStatus.Pending, resolution.ByVendor["v1"].LabelStatus);
        Assert.AreEqual(4, resolution.ByVendor["v1"].Cost);
        Assert.AreEqual("Return.Test", resolution.ByVendor["v1"].ProviderSystemName);
        _provider.Verify(x => x.ValidateForm(It.IsAny<ReturnShippingRequest>(), It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>()), Times.Never);
    }

    [TestMethod]
    public async Task GetReturnAddress_VendorWithAddress_UsesVendor()
    {
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(new Vendor
            { Id = "v1", Address = new Address { CountryId = "pl", Address1 = "Vendor street" } });
        _providerSettings.ReturnAddress = new Address { Address1 = "Returns street" };

        var address = await _service.GetReturnAddress(StoreId, "v1");

        Assert.AreEqual("Vendor street", address.Address1);
    }

    [TestMethod]
    public async Task GetReturnAddress_FallsBackToSettingsThenOrigin()
    {
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(new Vendor
            { Id = "v1", Address = new Address { Address1 = "No country" } });
        _providerSettings.ReturnAddress = new Address { Address1 = "Returns street" };

        Assert.AreEqual("Returns street", (await _service.GetReturnAddress(StoreId, "v1")).Address1);
        Assert.AreEqual("Returns street", (await _service.GetReturnAddress(StoreId, "")).Address1);

        _providerSettings.ReturnAddress = new Address();

        Assert.AreEqual("Origin", (await _service.GetReturnAddress(StoreId, "v1")).Address1);
    }
}
