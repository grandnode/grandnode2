using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Models.Payments;
using Grand.Web.AdminShared.Models.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class ShippingMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<ShippingMethodProfile>();
            cfg.AddProfile<ShippingSettingsProfile>();
            cfg.AddProfile<PaymentSettingsProfile>();
            cfg.AddProfile<CheckoutAttributeProfile>();
            cfg.AddProfile<AddressProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── ShippingMethod ────────────────────────────────────────────────────────

    [TestMethod]
    public Task ShippingMethod_ToModel()
    {
        var source = new ShippingMethod {
            Id = "sm-001",
            Name = "Standard Shipping",
            Description = "5-7 business days",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ShippingMethodModel>(source));
    }

    [TestMethod]
    public Task ShippingMethodModel_ToDomain()
    {
        var model = new ShippingMethodModel {
            Name = "Standard Shipping",
            Description = "5-7 business days",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ShippingMethod>(model));
    }

    // ── ShippingSettings ──────────────────────────────────────────────────────

    [TestMethod]
    public Task ShippingSettings_ToModel()
    {
        var source = new ShippingSettings {
            FreeShippingOverXEnabled = true,
            FreeShippingOverXValue = 100,
            FreeShippingOverXIncludingTax = false,
            EstimateShippingEnabled = true,
            DisplayShipmentEventsToCustomers = true,
            DisplayShipmentEventsToStoreOwner = false,
            SkipShippingMethodSelectionIfOnlyOne = true,
            AllowPickUpInStore = true
        };
        return Verify(_mapper.Map<ShippingSettingsModel>(source));
    }

    [TestMethod]
    public Task ShippingSettingsModel_ToDomain()
    {
        var model = new ShippingSettingsModel {
            FreeShippingOverXEnabled = true,
            FreeShippingOverXValue = 100,
            EstimateShippingEnabled = true,
            AllowPickUpInStore = true
        };
        return Verify(_mapper.Map<ShippingSettings>(model));
    }

    // ── PaymentSettings ───────────────────────────────────────────────────────

    [TestMethod]
    public Task PaymentSettings_ToModel()
    {
        var source = new PaymentSettings {
            AllowRePostingPayments = true,
            SkipPaymentIfOnlyOne = true,
            ShowPaymentDescriptions = true,
            SkipPaymentInfo = false
        };
        return Verify(_mapper.Map<PaymentSettingsModel>(source));
    }

    [TestMethod]
    public Task PaymentSettingsModel_ToDomain()
    {
        var model = new PaymentSettingsModel {
            AllowRePostingPayments = true,
            ShowPaymentDescriptions = true
        };
        return Verify(_mapper.Map<PaymentSettings>(model));
    }

    // ── CheckoutAttribute ─────────────────────────────────────────────────────

    [TestMethod]
    public Task CheckoutAttribute_ToModel()
    {
        var source = new CheckoutAttribute {
            Id = "ca-001",
            Name = "Gift Wrapping",
            TextPrompt = "Add gift wrapping?",
            IsRequired = false,
            AttributeControlTypeId = (AttributeControlType)4,
            DisplayOrder = 1,
            TaxCategoryId = "tc-001",
            ShippableProductRequired = false
        };
        return Verify(_mapper.Map<CheckoutAttributeModel>(source));
    }

    [TestMethod]
    public Task CheckoutAttributeModel_ToDomain()
    {
        var model = new CheckoutAttributeModel {
            Name = "Gift Wrapping",
            TextPrompt = "Add gift wrapping?",
            IsRequired = false,
            AttributeControlTypeId = 4,
            DisplayOrder = 1,
            CustomerGroups = ["grp-001"],
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<CheckoutAttribute>(model));
    }

    [TestMethod]
    public Task CheckoutAttributeValue_ToModel()
    {
        var source = new CheckoutAttributeValue {
            Id = "cav-001",
            Name = "Yes",
            PriceAdjustment = 3.99,
            WeightAdjustment = 0,
            IsPreSelected = false,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CheckoutAttributeValueModel>(source));
    }

    [TestMethod]
    public Task CheckoutAttributeValueModel_ToDomain()
    {
        var model = new CheckoutAttributeValueModel {
            Name = "Yes",
            PriceAdjustment = 3.99,
            IsPreSelected = false,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CheckoutAttributeValue>(model));
    }
}
