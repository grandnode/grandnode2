using Grand.Mapping;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Documents;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.AdminShared.Models.Documents;
using Grand.Web.AdminShared.Models.Localization;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Models.Permissions;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.AdminShared.Models.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class CommonMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<AddressProfile>();
            cfg.AddProfile<AddressAttributeProfile>();
            cfg.AddProfile<AddressAttributeValueProfile>();
            cfg.AddProfile<CountryProfile>();
            cfg.AddProfile<StateProvinceProfile>();
            cfg.AddProfile<CurrencyProfile>();
            cfg.AddProfile<LanguageProfile>();
            cfg.AddProfile<MeasureDimensionProfile>();
            cfg.AddProfile<MeasureWeightProfile>();
            cfg.AddProfile<MeasureUnitProfile>();
            cfg.AddProfile<StoreProfile>();
            cfg.AddProfile<DocumentProfile>();
            cfg.AddProfile<DocumentTypeProfile>();
            cfg.AddProfile<OrderStatusProfile>();
            cfg.AddProfile<MerchandiseReturnActionProfile>();
            cfg.AddProfile<MerchandiseReturnReasonProfile>();
            cfg.AddProfile<PickupPointProfile>();
            cfg.AddProfile<WarehouseProfile>();
            cfg.AddProfile<PermissionProfile>();
            cfg.AddProfile<TaxCategoryProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── Address ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Address_ToModel()
    {
        var source = new Address {
            Id = "addr-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Company = "ACME Corp",
            CountryId = "country-001",
            StateProvinceId = "state-001",
            City = "New York",
            Address1 = "123 Main St",
            Address2 = "Apt 4B",
            ZipPostalCode = "10001",
            PhoneNumber = "+1-555-1234",
            FaxNumber = "+1-555-5678"
        };
        return Verify(_mapper.Map<AddressModel>(source));
    }

    [TestMethod]
    public Task AddressModel_ToDomain()
    {
        var model = new AddressModel {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Company = "ACME Corp",
            CountryId = "country-001",
            StateProvinceId = "state-001",
            City = "New York",
            Address1 = "123 Main St",
            Address2 = "Apt 4B",
            ZipPostalCode = "10001",
            PhoneNumber = "+1-555-1234"
        };
        return Verify(_mapper.Map<Address>(model));
    }

    // ── AddressAttribute ──────────────────────────────────────────────────────

    [TestMethod]
    public Task AddressAttribute_ToModel()
    {
        var source = new AddressAttribute {
            Id = "aa-001",
            Name = "Custom Field",
            IsRequired = true,
            AttributeControlTypeId = 1,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<AddressAttributeModel>(source));
    }

    [TestMethod]
    public Task AddressAttributeModel_ToDomain()
    {
        var model = new AddressAttributeModel {
            Name = "Custom Field",
            IsRequired = true,
            AttributeControlTypeId = 1,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<AddressAttribute>(model));
    }

    // ── AddressAttributeValue ─────────────────────────────────────────────────

    [TestMethod]
    public Task AddressAttributeValue_ToModel()
    {
        var source = new AddressAttributeValue {
            Id = "aav-001",
            Name = "Option A",
            IsPreSelected = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<AddressAttributeValueModel>(source));
    }

    [TestMethod]
    public Task AddressAttributeValueModel_ToDomain()
    {
        var model = new AddressAttributeValueModel {
            Name = "Option A",
            IsPreSelected = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<AddressAttributeValue>(model));
    }

    // ── Country ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Country_ToModel()
    {
        var source = new Country {
            Id = "country-001",
            Name = "United States",
            AllowsBilling = true,
            AllowsShipping = true,
            TwoLetterIsoCode = "US",
            ThreeLetterIsoCode = "USA",
            NumericIsoCode = 840,
            SubjectToVat = false,
            Published = true,
            DisplayOrder = 1,
        };
        source.StateProvinces.Add(new StateProvince { Id = "sp-001", Name = "New York" });
        return Verify(_mapper.Map<CountryModel>(source));
    }

    [TestMethod]
    public Task CountryModel_ToDomain()
    {
        var model = new CountryModel {
            Name = "United States",
            AllowsBilling = true,
            AllowsShipping = true,
            TwoLetterIsoCode = "US",
            ThreeLetterIsoCode = "USA",
            NumericIsoCode = 840,
            Published = true,
            DisplayOrder = 1,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<Country>(model));
    }

    // ── StateProvince ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task StateProvince_ToModel()
    {
        var source = new StateProvince {
            Id = "sp-001",
            Name = "New York",
            Abbreviation = "NY",
            Published = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<StateProvinceModel>(source));
    }

    [TestMethod]
    public Task StateProvinceModel_ToDomain()
    {
        var model = new StateProvinceModel {
            Name = "New York",
            Abbreviation = "NY",
            Published = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<StateProvince>(model));
    }

    // ── Currency ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Currency_ToModel()
    {
        var source = new Currency {
            Id = "cur-001",
            Name = "US Dollar",
            CurrencyCode = "USD",
            Rate = 1.0,
            DisplayLocale = "en-US",
            CustomFormatting = "${0:N2}",
            Published = true,
            DisplayOrder = 1,
            RoundingTypeId = 0,
            MidpointRoundId = 0
        };
        return Verify(_mapper.Map<CurrencyModel>(source));
    }

    [TestMethod]
    public Task CurrencyModel_ToDomain()
    {
        var model = new CurrencyModel {
            Name = "US Dollar",
            CurrencyCode = "USD",
            Rate = 1.0,
            DisplayLocale = "en-US",
            Published = true,
            DisplayOrder = 1,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<Currency>(model));
    }

    // ── Language ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Language_ToModel()
    {
        var source = new Language {
            Id = "lang-001",
            Name = "English",
            LanguageCulture = "en-US",
            UniqueSeoCode = "en",
            FlagImageFileName = "us.png",
            Rtl = false,
            Published = true,
            DisplayOrder = 1,
            DefaultCurrencyId = "cur-001"
        };
        return Verify(_mapper.Map<LanguageModel>(source));
    }

    [TestMethod]
    public Task LanguageModel_ToDomain()
    {
        var model = new LanguageModel {
            Name = "English",
            LanguageCulture = "en-US",
            UniqueSeoCode = "en",
            FlagImageFileName = "us.png",
            Rtl = false,
            Published = true,
            DisplayOrder = 1,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<Language>(model));
    }

    // ── MeasureDimension ──────────────────────────────────────────────────────

    [TestMethod]
    public Task MeasureDimension_ToModel()
    {
        var source = new MeasureDimension { Id = "md-001", Name = "Centimeter", SystemKeyword = "cm", Ratio = 1.0, DisplayOrder = 1 };
        return Verify(_mapper.Map<MeasureDimensionModel>(source));
    }

    [TestMethod]
    public Task MeasureDimensionModel_ToDomain()
    {
        var model = new MeasureDimensionModel { Name = "Centimeter", SystemKeyword = "cm", Ratio = 1.0, DisplayOrder = 1 };
        return Verify(_mapper.Map<MeasureDimension>(model));
    }

    // ── MeasureWeight ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task MeasureWeight_ToModel()
    {
        var source = new MeasureWeight { Id = "mw-001", Name = "Kilogram", SystemKeyword = "kg", Ratio = 1.0, DisplayOrder = 1 };
        return Verify(_mapper.Map<MeasureWeightModel>(source));
    }

    [TestMethod]
    public Task MeasureWeightModel_ToDomain()
    {
        var model = new MeasureWeightModel { Name = "Kilogram", SystemKeyword = "kg", Ratio = 1.0, DisplayOrder = 1 };
        return Verify(_mapper.Map<MeasureWeight>(model));
    }

    // ── MeasureUnit ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task MeasureUnit_ToModel()
    {
        var source = new MeasureUnit { Id = "mu-001", Name = "Piece", DisplayOrder = 1 };
        return Verify(_mapper.Map<MeasureUnitModel>(source));
    }

    [TestMethod]
    public Task MeasureUnitModel_ToDomain()
    {
        var model = new MeasureUnitModel { Name = "Piece", DisplayOrder = 1 };
        return Verify(_mapper.Map<MeasureUnit>(model));
    }

    // ── Store ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Store_ToModel()
    {
        var source = new Store {
            Id = "store-001",
            Name = "Grand Store",
            Url = "https://grandstore.com",
            SslEnabled = true,
            CompanyName = "Grand LLC",
            CompanyAddress = "123 Commerce St",
            CompanyPhoneNumber = "+1-555-9999",
            CompanyVat = "US123456",
            DisplayOrder = 1,
            DefaultLanguageId = "lang-001",
            DefaultCurrencyId = "cur-001",
            DefaultCountryId = "country-001",
            Domains = [new DomainHost { Id = "dh-001", Url = "https://grandstore.com", Primary = true }],
            BankAccount = new BankAccount { BankName = "Chase", BankCode = "CHASUSU" }
        };
        return Verify(_mapper.Map<StoreModel>(source));
    }

    [TestMethod]
    public Task StoreModel_ToDomain()
    {
        var model = new StoreModel {
            Name = "Grand Store",
            Url = "https://grandstore.com",
            SslEnabled = true,
            CompanyName = "Grand LLC",
            DefaultLanguageId = "lang-001",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<Store>(model));
    }

    [TestMethod]
    public Task DomainHost_ToModel()
    {
        var source = new DomainHost { Id = "dh-001", Url = "https://example.com", Primary = true };
        return Verify(_mapper.Map<DomainHostModel>(source));
    }

    [TestMethod]
    public Task DomainHostModel_ToDomain()
    {
        var model = new DomainHostModel { Url = "https://example.com", Primary = true };
        return Verify(_mapper.Map<DomainHost>(model));
    }

    [TestMethod]
    public Task BankAccount_ToModel()
    {
        var source = new BankAccount { BankName = "Chase", BankCode = "CHASE123", SwiftCode = "CHASUS33", AccountNumber = "123456789" };
        return Verify(_mapper.Map<StoreModel.BankAccountModel>(source));
    }

    // ── Document ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Document_ToModel()
    {
        var source = new Document {
            Id = "doc-001",
            Name = "Invoice #001",
            Number = "INV-001",
            DocumentTypeId = "dt-001",
            CustomerId = "cust-001",
            ReferenceId = Reference.Order,
            StatusId = DocumentStatus.Open,
            DisplayOrder = 1,
            Published = true,
            TotalAmount = 500,
            OutstandAmount = 0,
            CurrencyCode = "USD"
        };
        return Verify(_mapper.Map<DocumentModel>(source));
    }

    [TestMethod]
    public Task DocumentModel_ToDomain()
    {
        var model = new DocumentModel {
            Name = "Invoice #001",
            Number = "INV-001",
            DocumentTypeId = "dt-001",
            Published = true,
            CustomerGroups = ["grp-001"],
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<Document>(model));
    }

    // ── DocumentType ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task DocumentType_ToModel()
    {
        var source = new DocumentType { Id = "dt-001", Name = "Invoice", Description = "An invoice document", DisplayOrder = 1 };
        return Verify(_mapper.Map<DocumentTypeModel>(source));
    }

    [TestMethod]
    public Task DocumentTypeModel_ToDomain()
    {
        var model = new DocumentTypeModel { Name = "Invoice", Description = "An invoice document", DisplayOrder = 1 };
        return Verify(_mapper.Map<DocumentType>(model));
    }

    // ── OrderStatus ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task OrderStatus_ToModel()
    {
        var source = new OrderStatus { Id = "os-001", Name = "Processing", DisplayOrder = 2 };
        return Verify(_mapper.Map<OrderStatusModel>(source));
    }

    [TestMethod]
    public Task OrderStatusModel_ToDomain()
    {
        var model = new OrderStatusModel { Name = "Processing", DisplayOrder = 2 };
        return Verify(_mapper.Map<OrderStatus>(model));
    }

    // ── MerchandiseReturnAction ───────────────────────────────────────────────

    [TestMethod]
    public Task MerchandiseReturnAction_ToModel()
    {
        var source = new MerchandiseReturnAction { Id = "mra-001", Name = "Refund", DisplayOrder = 1 };
        return Verify(_mapper.Map<MerchandiseReturnActionModel>(source));
    }

    [TestMethod]
    public Task MerchandiseReturnActionModel_ToDomain()
    {
        var model = new MerchandiseReturnActionModel { Name = "Refund", DisplayOrder = 1 };
        return Verify(_mapper.Map<MerchandiseReturnAction>(model));
    }

    // ── MerchandiseReturnReason ───────────────────────────────────────────────

    [TestMethod]
    public Task MerchandiseReturnReason_ToModel()
    {
        var source = new MerchandiseReturnReason { Id = "mrr-001", Name = "Defective", DisplayOrder = 1 };
        return Verify(_mapper.Map<MerchandiseReturnReasonModel>(source));
    }

    [TestMethod]
    public Task MerchandiseReturnReasonModel_ToDomain()
    {
        var model = new MerchandiseReturnReasonModel { Name = "Defective", DisplayOrder = 1 };
        return Verify(_mapper.Map<MerchandiseReturnReason>(model));
    }

    // ── PickupPoint ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task PickupPoint_ToModel()
    {
        var source = new PickupPoint {
            Id = "pp-001",
            Name = "Main Store Pickup",
            Description = "Pickup at main store",
            AdminComment = "Note",
            PickupFee = 2.00,
            DisplayOrder = 1,
            StoreId = "store-001",
            Latitude = 40.7128,
            Longitude = -74.0060
        };
        return Verify(_mapper.Map<PickupPointModel>(source));
    }

    [TestMethod]
    public Task PickupPointModel_ToDomain()
    {
        var model = new PickupPointModel {
            Name = "Main Store Pickup",
            PickupFee = 2.00,
            DisplayOrder = 1,
            StoreId = "store-001"
        };
        return Verify(_mapper.Map<PickupPoint>(model));
    }

    // ── Warehouse ─────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Warehouse_ToModel()
    {
        var source = new Warehouse {
            Id = "wh-001",
            Name = "Main Warehouse",
            AdminComment = "Primary warehouse",
            Code = "WH-MAIN",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<WarehouseModel>(source));
    }

    [TestMethod]
    public Task WarehouseModel_ToDomain()
    {
        var model = new WarehouseModel {
            Name = "Main Warehouse",
            AdminComment = "Primary",
            Code = "WH-MAIN",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<Warehouse>(model));
    }

    // ── Permission ────────────────────────────────────────────────────────────

    [TestMethod]
    public Task PermissionCreateModel_ToDomain()
    {
        var model = new PermissionCreateModel {
            Name = "Manage Products",
            SystemName = "ManageProducts",
            Area = "Admin",
            Category = "Catalog"
        };
        return Verify(_mapper.Map<Permission>(model));
    }

    [TestMethod]
    public Task Permission_ToUpdateModel()
    {
        var source = new Permission {
            Id = "perm-001",
            Name = "Manage Products",
            SystemName = "ManageProducts",
            Area = "Admin",
            Category = "Catalog"
        };
        return Verify(_mapper.Map<PermissionUpdateModel>(source));
    }

    // ── TaxCategory ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task TaxCategory_ToModel()
    {
        var source = new Grand.Domain.Tax.TaxCategory { Id = "tc-001", Name = "Standard Rate", DisplayOrder = 1 };
        return Verify(_mapper.Map<Grand.Web.AdminShared.Models.Tax.TaxCategoryModel>(source));
    }

    [TestMethod]
    public Task TaxCategoryModel_ToDomain()
    {
        var model = new Grand.Web.AdminShared.Models.Tax.TaxCategoryModel { Name = "Standard Rate", DisplayOrder = 1 };
        return Verify(_mapper.Map<Grand.Domain.Tax.TaxCategory>(model));
    }
}
