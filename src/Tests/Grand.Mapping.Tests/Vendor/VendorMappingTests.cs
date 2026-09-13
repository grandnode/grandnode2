using Grand.Mapping;
using Grand.Domain.Common;
using Grand.Web.Vendor.Mapper;
using Grand.Web.Vendor.Models.Common;
using Grand.Web.Vendor.Models.Vendor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;
using AddressProfile = Grand.Web.Vendor.Mapper.AddressProfile;
using VendorProfile = Grand.Web.Vendor.Mapper.VendorProfile;

namespace Grand.Mapping.Tests.Vendor;

// Product-related cases (Product/ProductAttributeMapping/ProductAttributeCombination <-> Vendor's
// Models.Catalog) were removed here (ARCH-001 Phase 1 Task 13): the Vendor.Mapper.ProductProfile they
// exercised, and the Vendor.Models.Catalog types they mapped to/from, were deleted as orphans once
// Task 12 repointed Vendor's _ViewImports.cshtml to AdminShared's models/service. The equivalent
// coverage - mapping Product/ProductAttributeMapping/ProductAttributeCombination to/from AdminShared's
// ProductModel via AdminShared's ProductProfile, which Vendor now uses - lives in
// Grand.Mapping.Tests.AdminShared.CatalogProductMappingTests.
[TestClass]
public class VendorMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<AddressProfile>();
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
}
