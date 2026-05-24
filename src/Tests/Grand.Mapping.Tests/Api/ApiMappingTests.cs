using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Infrastructure.Mapper.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;
using AddressProfile = Grand.Module.Api.Infrastructure.Mapper.Profiles.AddressProfile;
using BrandProfile = Grand.Module.Api.Infrastructure.Mapper.Profiles.BrandProfile;
using CategoryProfile = Grand.Module.Api.Infrastructure.Mapper.Profiles.CategoryProfile;
using CollectionProfile = Grand.Module.Api.Infrastructure.Mapper.Profiles.CollectionProfile;
using SpecificationAttributeProfile = Grand.Module.Api.Infrastructure.Mapper.Profiles.SpecificationAttributeProfile;
using TierPriceProfile = Grand.Module.Api.Infrastructure.Mapper.Profiles.TierPriceProfile;

namespace Grand.Mapping.Tests.Api;

[TestClass]
public class ApiMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<AddressProfile>();
            cfg.AddProfile<BrandProfile>();
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<CollectionProfile>();
            cfg.AddProfile<CustomerGroupProfile>();
            cfg.AddProfile<CustomerProfile>();
            cfg.AddProfile<PictureProfile>();
            cfg.AddProfile<ProductAttributeMappingProfile>();
            cfg.AddProfile<ProductAttributeProfile>();
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<SpecificationAttributeProfile>();
            cfg.AddProfile<TierPriceProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── Address ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task AddressDto_ToEntity()
    {
        var dto = new AddressDto {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "123-456-7890",
            Address1 = "123 Main St",
            City = "Springfield",
            ZipPostalCode = "12345",
            CountryId = "US"
        };
        return Verify(_mapper.Map<Address>(dto));
    }

    [TestMethod]
    public Task Address_ToDto()
    {
        var entity = new Address {
            Id = "addr-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "123-456-7890",
            Address1 = "123 Main St",
            City = "Springfield",
            ZipPostalCode = "12345",
            CountryId = "US"
        };
        return Verify(_mapper.Map<AddressDto>(entity));
    }

    // ── Brand ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task BrandDto_ToEntity()
    {
        var dto = new BrandDto {
            Id = "brand-1",
            Name = "Nike",
            SeName = "nike",
            Description = "Sports brand",
            MetaTitle = "Nike",
            PageSize = 12,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24",
            ShowOnHomePage = true,
            Published = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<Brand>(dto));
    }

    [TestMethod]
    public Task Brand_ToDto()
    {
        var entity = new Brand {
            Id = "brand-1",
            Name = "Nike",
            SeName = "nike",
            Description = "Sports brand",
            MetaTitle = "Nike",
            PageSize = 12,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24",
            ShowOnHomePage = true,
            Published = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<BrandDto>(entity));
    }

    // ── Category ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task CategoryDto_ToEntity()
    {
        var dto = new CategoryDto {
            Id = "cat-1",
            Name = "Electronics",
            SeName = "electronics",
            Description = "Electronic items",
            MetaTitle = "Electronics",
            PageSize = 9,
            AllowCustomersToSelectPageSize = true,
            Published = true,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<Category>(dto));
    }

    [TestMethod]
    public Task Category_ToDto()
    {
        var entity = new Category {
            Id = "cat-1",
            Name = "Electronics",
            SeName = "electronics",
            Description = "Electronic items",
            MetaTitle = "Electronics",
            PageSize = 9,
            AllowCustomersToSelectPageSize = true,
            Published = true,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<CategoryDto>(entity));
    }

    // ── Collection ────────────────────────────────────────────────────────────

    [TestMethod]
    public Task CollectionDto_ToEntity()
    {
        var dto = new CollectionDto {
            Id = "col-1",
            Name = "Summer 2025",
            SeName = "summer-2025",
            Description = "Summer collection",
            PageSize = 12,
            Published = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<Collection>(dto));
    }

    [TestMethod]
    public Task Collection_ToDto()
    {
        var entity = new Collection {
            Id = "col-1",
            Name = "Summer 2025",
            SeName = "summer-2025",
            Description = "Summer collection",
            PageSize = 12,
            Published = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CollectionDto>(entity));
    }

    // ── CustomerGroup ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerGroupDto_ToEntity()
    {
        var dto = new CustomerGroupDto {
            Id = "cg-1",
            Name = "VIP Customers",
            SystemName = "Vip",
            FreeShipping = true
        };
        return Verify(_mapper.Map<CustomerGroup>(dto));
    }

    [TestMethod]
    public Task CustomerGroup_ToDto()
    {
        var entity = new CustomerGroup {
            Id = "cg-1",
            Name = "VIP Customers",
            SystemName = "Vip",
            FreeShipping = true
        };
        return Verify(_mapper.Map<CustomerGroupDto>(entity));
    }

    // ── Customer ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerDto_ToEntity()
    {
        var dto = new CustomerDto {
            Id = "cust-1",
            Email = "customer@example.com",
            Username = "customer1",
            Active = true
        };
        return Verify(_mapper.Map<Customer>(dto));
    }

    [TestMethod]
    public Task Customer_ToDto_EmptyUserFields()
    {
        var entity = new Customer {
            Id = "cust-1",
            Email = "customer@example.com",
            Username = "customer1",
            Active = true
        };
        return Verify(_mapper.Map<CustomerDto>(entity));
    }

    [TestMethod]
    public Task Customer_ToDto_WithUserFields()
    {
        var entity = new Customer {
            Id = "cust-1",
            Email = "customer@example.com",
            Username = "customer1",
            Active = true,
            UserFields = [
                new UserField { Key = "FirstName", Value = "John", StoreId = "" },
                new UserField { Key = "LastName", Value = "Doe", StoreId = "" },
                new UserField { Key = "Gender", Value = "M", StoreId = "" },
                new UserField { Key = "City", Value = "New York", StoreId = "" },
                new UserField { Key = "Company", Value = "Acme Corp", StoreId = "" },
                new UserField { Key = "StreetAddress", Value = "123 Main St", StoreId = "" },
                new UserField { Key = "StreetAddress2", Value = "Apt 4", StoreId = "" },
                new UserField { Key = "ZipPostalCode", Value = "10001", StoreId = "" },
                new UserField { Key = "CountryId", Value = "country-001", StoreId = "" },
                new UserField { Key = "StateProvinceId", Value = "state-001", StoreId = "" },
                new UserField { Key = "Phone", Value = "+1-555-0123", StoreId = "" },
                new UserField { Key = "Fax", Value = "+1-555-0124", StoreId = "" },
                new UserField { Key = "VatNumber", Value = "VAT123456", StoreId = "" },
                new UserField { Key = "VatNumberStatusId", Value = "20", StoreId = "" }
            ]
        };
        return Verify(_mapper.Map<CustomerDto>(entity));
    }

    // ── Picture ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task PictureDto_ToEntity()
    {
        var dto = new PictureDto {
            Id = "pic-1",
            MimeType = "image/jpeg",
            SeoFilename = "product-image",
            AltAttribute = "Product image",
            TitleAttribute = "Product"
        };
        return Verify(_mapper.Map<Picture>(dto));
    }

    [TestMethod]
    public Task Picture_ToDto()
    {
        var entity = new Picture {
            Id = "pic-1",
            MimeType = "image/jpeg",
            SeoFilename = "product-image",
            AltAttribute = "Product image",
            TitleAttribute = "Product"
        };
        return Verify(_mapper.Map<PictureDto>(entity));
    }

    // ── ProductAttribute ──────────────────────────────────────────────────────

    [TestMethod]
    public Task ProductAttributeDto_ToEntity()
    {
        var dto = new ProductAttributeDto {
            Id = "pa-1",
            Name = "Color",
            Description = "Product color"
        };
        return Verify(_mapper.Map<ProductAttribute>(dto));
    }

    [TestMethod]
    public Task ProductAttribute_ToDto()
    {
        var entity = new ProductAttribute {
            Id = "pa-1",
            Name = "Color",
            Description = "Product color"
        };
        return Verify(_mapper.Map<ProductAttributeDto>(entity));
    }

    [TestMethod]
    public Task PredefinedProductAttributeValue_ToDto()
    {
        var entity = new PredefinedProductAttributeValue {
            Id = "ppav-1",
            Name = "Red",
            PriceAdjustment = 0,
            WeightAdjustment = 0,
            IsPreSelected = false,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<PredefinedProductAttributeValueDto>(entity));
    }

    // ── ProductAttributeMapping ───────────────────────────────────────────────

    [TestMethod]
    public Task ProductAttributeMappingDto_ToEntity()
    {
        var dto = new ProductAttributeMappingDto {
            Id = "pam-1",
            ProductAttributeId = "pa-1",
            TextPrompt = "Choose color",
            IsRequired = true,
            AttributeControlTypeId = (AttributeControlType)1,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<ProductAttributeMapping>(dto));
    }

    [TestMethod]
    public Task ProductAttributeMapping_ToDto()
    {
        var entity = new ProductAttributeMapping {
            Id = "pam-1",
            ProductAttributeId = "pa-1",
            TextPrompt = "Choose color",
            IsRequired = true,
            AttributeControlTypeId = (AttributeControlType)1,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<ProductAttributeMappingDto>(entity));
    }

    // ── SpecificationAttribute ────────────────────────────────────────────────

    [TestMethod]
    public Task SpecificationAttributeDto_ToEntity()
    {
        var dto = new SpecificationAttributeDto {
            Id = "sa-1",
            Name = "Material",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<SpecificationAttribute>(dto));
    }

    [TestMethod]
    public Task SpecificationAttribute_ToDto()
    {
        var entity = new SpecificationAttribute {
            Id = "sa-1",
            Name = "Material",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<SpecificationAttributeDto>(entity));
    }

    [TestMethod]
    public Task SpecificationAttributeOption_ToDto()
    {
        var entity = new SpecificationAttributeOption {
            Id = "sao-1",
            Name = "Cotton",
            DisplayOrder = 0,
            ColorSquaresRgb = "#FFFFFF"
        };
        return Verify(_mapper.Map<SpecificationAttributeOptionDto>(entity));
    }

    // ── TierPrice ─────────────────────────────────────────────────────────────

    [TestMethod]
    public Task ProductTierPriceDto_ToEntity()
    {
        var dto = new ProductTierPriceDto {
            Id = "tp-1",
            StoreId = "",
            CustomerGroupId = "",
            Quantity = 10,
            Price = 9.99,
            StartDateTimeUtc = null,
            EndDateTimeUtc = null
        };
        return Verify(_mapper.Map<TierPrice>(dto));
    }

    [TestMethod]
    public Task TierPrice_ToDto()
    {
        var entity = new TierPrice {
            Id = "tp-1",
            StoreId = "",
            CustomerGroupId = "",
            Quantity = 10,
            Price = 9.99
        };
        return Verify(_mapper.Map<ProductTierPriceDto>(entity));
    }

    // ── Product ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task ProductDto_ToEntity()
    {
        var dto = new ProductDto {
            Id = "prod-1",
            Name = "Test Product",
            ShortDescription = "Short",
            FullDescription = "Full description",
            Sku = "SKU001",
            Price = 29.99,
            Published = true,
            StockQuantity = 100,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<Product>(dto));
    }

    [TestMethod]
    public Task Product_ToDto()
    {
        var entity = new Product {
            Id = "prod-1",
            Name = "Test Product",
            ShortDescription = "Short",
            FullDescription = "Full description",
            Sku = "SKU001",
            Price = 29.99,
            Published = true,
            StockQuantity = 100,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<ProductDto>(entity));
    }
}
