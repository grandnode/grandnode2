using Grand.Business.Checkout.Services.Shipping.ReturnProviders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Shipping.ReturnProviders;

[TestClass]
public class SendYourselfReturnProviderTests
{
    private IReturnShippingProvider _provider;
    private Mock<ISettingService> _settingService;
    private Mock<IVendorService> _vendorService;

    [TestInitialize]
    public void Init()
    {
        _settingService = new Mock<ISettingService>();
        _settingService.Setup(x => x.LoadSetting<ReturnShippingProviderSettings>("s1"))
            .ReturnsAsync(new ReturnShippingProviderSettings { SendYourselfCost = 4 });

        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>(), "l1", It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string _, bool _) => key.EndsWith("Instructions")
                ? "Send the parcel to: {0}. Write return number {1} on it."
                : key);
        var orderService = new Mock<IOrderService>();
        orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1", CustomerLanguageId = "l1" });
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("s1")).ReturnsAsync(new Store { Id = "s1" });
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById("l1")).ReturnsAsync(new Language { Id = "l1" });
        var countryService = new Mock<ICountryService>();
        countryService.Setup(x => x.GetCountryById("pl")).ReturnsAsync(new Country { Id = "pl", Name = "Poland" });

        _vendorService = new Mock<IVendorService>();
        _provider = new SendYourselfReturnProvider(_settingService.Object, translationService.Object,
            orderService.Object, storeService.Object, languageService.Object, countryService.Object,
            _vendorService.Object);
    }

    [TestMethod]
    public async Task SendYourself_CostFromStoreSettings()
    {
        var options = await _provider.GetReturnOptions(new ReturnShippingRequest { StoreId = "s1", LanguageId = "l1" });

        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("send", options[0].Code);
        Assert.AreEqual(4, options[0].Cost);
        Assert.IsFalse(options[0].HasForm);
        Assert.IsFalse(options[0].CreatesLabel);
    }

    [TestMethod]
    public async Task SendYourself_CreateLabel_InstructionsHaveAddressAndNumber()
    {
        var merchandiseReturn = new MerchandiseReturn {
            OrderId = "o1", StoreId = "s1", ReturnNumber = 17,
            ReturnShipping = new MerchandiseReturnShipping {
                ReturnAddress = new Address
                    { Address1 = "Main 1", ZipPostalCode = "00-001", City = "Warsaw", CountryId = "pl" }
            }
        };

        var result = await _provider.CreateLabel(merchandiseReturn);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Send the parcel to: Main 1, 00-001 Warsaw, Poland. Write return number 17 on it.",
            result.Instructions);
    }

    [TestMethod]
    [DataRow("Shop Ltd", "Jan", "Kowalski", "Shop Ltd")]
    [DataRow("", "Jan", "Kowalski", "Jan Kowalski")]
    [DataRow(null, null, "Kowalski", "Kowalski")]
    public async Task SendYourself_CreateLabel_InstructionsStartWithTheRecipient(string company, string firstName,
        string lastName, string recipient)
    {
        var merchandiseReturn = new MerchandiseReturn {
            OrderId = "o1", StoreId = "s1", ReturnNumber = 17,
            ReturnShipping = new MerchandiseReturnShipping {
                ReturnAddress = new Address {
                    Company = company, FirstName = firstName, LastName = lastName, Address1 = "Main 1",
                    ZipPostalCode = "00-001", City = "Warsaw", CountryId = "pl"
                }
            }
        };

        var result = await _provider.CreateLabel(merchandiseReturn);

        Assert.AreEqual($"Send the parcel to: {recipient}, Main 1, 00-001 Warsaw, Poland. Write return number 17 on it.",
            result.Instructions);
    }

    private static MerchandiseReturn VendorReturn()
    {
        //the vendor's address, copied without a company or a name
        return new MerchandiseReturn {
            OrderId = "o1", StoreId = "s1", ReturnNumber = 17, VendorId = "v1",
            ReturnShipping = new MerchandiseReturnShipping {
                ReturnAddress = new Address
                    { Address1 = "Main 1", ZipPostalCode = "00-001", City = "Warsaw", CountryId = "pl" }
            }
        };
    }

    [TestMethod]
    public async Task SendYourself_CreateLabel_VendorAddressWithoutNames_StartsWithTheVendorName()
    {
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(new Vendor {
            Id = "v1", Name = "Vendor One",
            Address = new Address { Address1 = "Main 1", ZipPostalCode = "00-001", City = "Warsaw", CountryId = "pl" }
        });

        var result = await _provider.CreateLabel(VendorReturn());

        Assert.AreEqual("Send the parcel to: Vendor One, Main 1, 00-001 Warsaw, Poland. Write return number 17 on it.",
            result.Instructions);
    }

    [TestMethod]
    public async Task SendYourself_CreateLabel_ReturnKeptTheStoreAddress_NotNamedForTheVendor()
    {
        //the vendor has an address now, but the return was submitted while it had none: the parcel goes to the store
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(new Vendor {
            Id = "v1", Name = "Vendor One",
            Address = new Address { Address1 = "Vendor street 9", ZipPostalCode = "11-111", CountryId = "pl" }
        });

        var result = await _provider.CreateLabel(VendorReturn());

        Assert.AreEqual("Send the parcel to: Main 1, 00-001 Warsaw, Poland. Write return number 17 on it.",
            result.Instructions);
    }

    [TestMethod]
    public async Task SendYourself_CreateLabel_VendorWithoutAddress_StoreAddressNotNamedForTheVendor()
    {
        //the vendor has no return address of its own: the parcel goes to the store's, not to the vendor
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(new Vendor { Id = "v1", Name = "Vendor One" });

        var result = await _provider.CreateLabel(VendorReturn());

        Assert.AreEqual("Send the parcel to: Main 1, 00-001 Warsaw, Poland. Write return number 17 on it.",
            result.Instructions);
    }

    [TestMethod]
    public void SendYourself_Identity()
    {
        Assert.AreEqual("ReturnShipping.SendYourself", _provider.SystemName);
        Assert.AreEqual(0, _provider.Priority);
        Assert.IsNull(_provider.ShipmentTracker);
    }
}
