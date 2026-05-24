using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Vendor.Mapper;
using Grand.Web.Vendor.Models.Catalog;
using Grand.Web.Vendor.Models.Common;
using Grand.Web.Vendor.Models.Vendor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;
using AddressProfile = Grand.Web.Vendor.Mapper.AddressProfile;
using ProductProfile = Grand.Web.Vendor.Mapper.ProductProfile;
using VendorProfile = Grand.Web.Vendor.Mapper.VendorProfile;

namespace Grand.Mapping.Tests.Vendor;

[TestClass]
public class VendorMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<AddressProfile>();
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<VendorProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── Address ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Address_ToVendorAddressModel()
    {
        var entity = new Address {
            Id = "addr-v-1",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@vendor.com",
            PhoneNumber = "555-1234",
            Address1 = "42 Commerce St",
            City = "Tradeville",
            ZipPostalCode = "99001",
            CountryId = "GB"
        };
        return Verify(_mapper.Map<AddressModel>(entity));
    }

    // ── Vendor ────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Vendor_ToVendorModel()
    {
        var entity = new Grand.Domain.Vendors.Vendor {
            Id = "vendor-1",
            Name = "Test Vendor",
            SeName = "test-vendor",
            Description = "Quality products",
            Email = "vendor@example.com",
            MetaTitle = "Test Vendor",
            MetaKeywords = "vendor, test",
            MetaDescription = "Best vendor",
            Active = true,
            Deleted = false,
            DisplayOrder = 0,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24",
            AllowCustomerReviews = true
        };
        return Verify(_mapper.Map<VendorModel>(entity));
    }

    [TestMethod]
    public Task VendorModel_ToVendor()
    {
        var model = new VendorModel {
            Name = "Updated Vendor",
            Description = "Updated description",
            Email = "updated@example.com",
            MetaTitle = "Updated Vendor",
            MetaKeywords = "vendor, updated",
            PageSize = 12,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24"
        };
        return Verify(_mapper.Map<Grand.Domain.Vendors.Vendor>(model));
    }

    // ── Product ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Product_ToVendorProductModel()
    {
        var entity = new Product {
            Id = "prod-v-1",
            Name = "Vendor Product",
            ShortDescription = "Short desc",
            FullDescription = "Full description",
            Sku = "VSKU001",
            Price = 49.99,
            OldPrice = 59.99,
            Published = true,
            StockQuantity = 50,
            Weight = 1.5,
            DisplayOrder = 1,
            ProductTypeId = (ProductType)5
        };
        return Verify(_mapper.Map<ProductModel>(entity));
    }

    [TestMethod]
    public Task VendorProductModel_ToProduct()
    {
        var model = new ProductModel {
            Name = "New Product",
            ShortDescription = "Short",
            FullDescription = "Full",
            Sku = "SKU002",
            Price = 39.99,
            Published = true,
            StockQuantity = 25
        };
        return Verify(_mapper.Map<Product>(model));
    }

    // ── ProductAttributeMapping ───────────────────────────────────────────────

    [TestMethod]
    public Task ProductAttributeMapping_ToVendorModel()
    {
        var entity = new ProductAttributeMapping {
            Id = "pam-v-1",
            ProductAttributeId = "pa-1",
            TextPrompt = "Choose size",
            IsRequired = true,
            AttributeControlTypeId = (AttributeControlType)1,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<ProductModel.ProductAttributeMappingModel>(entity));
    }

    // ── ProductAttributeCombination ───────────────────────────────────────────

    [TestMethod]
    public Task ProductAttributeCombination_ToVendorModel()
    {
        var entity = new ProductAttributeCombination {
            Id = "pac-v-1",
            Sku = "COMB-SKU",
            StockQuantity = 5,
            AllowOutOfStockOrders = false,
            OverriddenPrice = null,
            NotifyAdminForQuantityBelow = 1
        };
        return Verify(_mapper.Map<ProductAttributeCombinationModel>(entity));
    }
}
