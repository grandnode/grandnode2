using Grand.Business.Checkout.Services.Shipping.ReturnProviders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Shipping.ReturnProviders;

[TestClass]
public class InStoreReturnProviderTests
{
    private Mock<IPickupPointService> _pickupPointService;
    private IReturnShippingProvider _provider;

    [TestInitialize]
    public void Init()
    {
        _pickupPointService = new Mock<IPickupPointService>();
        _pickupPointService.Setup(x => x.LoadActivePickupPoints("s1")).ReturnsAsync([
            new PickupPoint {
                Id = "p1", Name = "Shop", StoreId = "s1", WarehouseId = "w1",
                Address = new Address { Address1 = "Main 1" }
            }
        ]);
        _pickupPointService.Setup(x => x.LoadActivePickupPoints("s2")).ReturnsAsync([]);

        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>(), "l1", It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string _, bool _) => key.EndsWith("Instructions")
                ? "Bring the items with return number {1} to {0}."
                : key);
        var orderService = new Mock<IOrderService>();
        orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1", CustomerLanguageId = "l1" });
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("s1")).ReturnsAsync(new Store { Id = "s1" });
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById("l1")).ReturnsAsync(new Language { Id = "l1" });

        _provider = new InStoreReturnProvider(_pickupPointService.Object, translationService.Object,
            orderService.Object, storeService.Object, languageService.Object, new Mock<ICountryService>().Object);
    }

    [TestMethod]
    public async Task InStore_StoreReturnWithPoints_OffersOptionWithForm()
    {
        var options = await _provider.GetReturnOptions(new ReturnShippingRequest
            { StoreId = "s1", VendorId = "", LanguageId = "l1" });

        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("instore", options[0].Code);
        Assert.AreEqual(0, options[0].Cost);
        Assert.IsTrue(options[0].HasForm);
        Assert.AreEqual("MerchandiseReturnInStorePoints", await _provider.GetFormRouteName("instore"));
    }

    [TestMethod]
    public async Task InStore_VendorReturn_NoOption()
    {
        var options = await _provider.GetReturnOptions(new ReturnShippingRequest { StoreId = "s1", VendorId = "v1" });

        Assert.AreEqual(0, options.Count);
    }

    [TestMethod]
    public async Task InStore_NoActivePoints_NoOption()
    {
        var options = await _provider.GetReturnOptions(new ReturnShippingRequest { StoreId = "s2", VendorId = "" });

        Assert.AreEqual(0, options.Count);
    }

    [TestMethod]
    public async Task InStore_ValidateForm_PointOfOtherStore_Error()
    {
        var result = await _provider.ValidateForm(new ReturnShippingRequest { StoreId = "s2", LanguageId = "l1" },
            "instore", new Dictionary<string, string> { ["pickupPointId"] = "p1" });

        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual("MerchandiseReturns.ReturnShipping.InStore.ChoosePoint", result.Errors[0]);
        Assert.IsNull(result.DropOffPoint);
    }

    [TestMethod]
    public async Task InStore_ValidateForm_NoPoint_Error()
    {
        var result = await _provider.ValidateForm(new ReturnShippingRequest { StoreId = "s1", LanguageId = "l1" },
            "instore", new Dictionary<string, string>());

        Assert.AreEqual(1, result.Errors.Count);
    }

    [TestMethod]
    public async Task InStore_ValidateForm_SnapshotHasWarehouse()
    {
        var result = await _provider.ValidateForm(new ReturnShippingRequest { StoreId = "s1", LanguageId = "l1" },
            "instore", new Dictionary<string, string> { ["pickupPointId"] = "p1" });

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual("p1", result.DropOffPoint.Id);
        Assert.AreEqual("w1", result.DropOffPoint.WarehouseId);
        Assert.AreEqual("Main 1", result.DropOffPoint.Address.Address1);
    }

    [TestMethod]
    public async Task InStore_CreateLabel_InstructionsHavePointAndNumber()
    {
        var result = await _provider.CreateLabel(new MerchandiseReturn {
            OrderId = "o1", StoreId = "s1", ReturnNumber = 5,
            ReturnShipping = new MerchandiseReturnShipping {
                DropOffPoint = new ReturnDropOffPoint { Address = new Address { Address1 = "Main 1" } }
            }
        });

        Assert.AreEqual("Bring the items with return number 5 to Main 1.", result.Instructions);
    }
}
