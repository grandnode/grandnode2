using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class CatalogProductMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<TierPriceProfile>();
            cfg.AddProfile<ProductSpecificationProfile>();
            cfg.AddProfile<ProductAttributeProfile>();
            cfg.AddProfile<PredefinedProductAttributeValueProfile>();
            cfg.AddProfile<ProductAttributeMappingProfile>();
            cfg.AddProfile<ProductAttributeCombinationProfile>();
            cfg.AddProfile<ProductReviewProfile>();
            cfg.AddProfile<SpecificationAttributeProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── Product ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Product_ToModel()
    {
        var source = new Product {
            Id = "prod-001",
            Name = "Laptop Pro",
            ShortDescription = "High-performance laptop",
            FullDescription = "A full description of the laptop",
            AdminComment = "Internal note",
            ProductLayoutId = "pl-001",
            BrandId = "brand-001",
            VendorId = "vendor-001",
            ShowOnHomePage = true,
            MetaKeywords = "laptop",
            MetaDescription = "Buy laptop",
            MetaTitle = "Laptop",
            AllowCustomerReviews = true,
            Published = true,
            VisibleIndividually = true,
            Price = 999.99,
            OldPrice = 1199.99,
            ProductCost = 600,
            MarkAsNew = false,
            Weight = 2.5,
            Length = 35,
            Width = 24,
            Height = 2,
            DisplayOrder = 1,
            DisplayStockQuantity = true,
            ManageInventoryMethodId = (ManageInventoryMethod)1,
            StockQuantity = 50,
            Sku = "LAPTOP-001",
            Gtin = "1234567890",
            SeName = "laptop-pro"
        };
        return Verify(_mapper.Map<ProductModel>(source));
    }

    [TestMethod]
    public Task ProductModel_ToDomain()
    {
        var model = new ProductModel {
            Id = "prod-001",
            Name = "Laptop Pro",
            ShortDescription = "High-performance laptop",
            FullDescription = "Full description",
            Published = true,
            VisibleIndividually = true,
            Price = 999.99,
            OldPrice = 1199.99,
            Sku = "LAPTOP-001",
            Weight = 2.5,
            Length = 35,
            Width = 24,
            Height = 2,
            StockQuantity = 50,
            DisplayOrder = 1,
            SeName = "laptop-pro"
        };
        return Verify(_mapper.Map<Product>(model));
    }

    [TestMethod]
    public Task Product_ToModel_WithCalendarData()
    {
        var source = new Product {
            Id = "prod-002",
            Name = "Calendar Product",
            Sku = "CAL-001",
            IncBothDate = true
        };
        return Verify(_mapper.Map<ProductModel>(source));
    }

    [TestMethod]
    public Task ProductModel_ToDomain_WithCalendarData()
    {
        var model = new ProductModel {
            Name = "Calendar Product",
            Sku = "CAL-001",
            CalendarModel = new ProductModel.GenerateCalendarModel {
                IncBothDate = true
            }
        };
        return Verify(_mapper.Map<Product>(model));
    }

    // ── TierPrice ─────────────────────────────────────────────────────────────

    [TestMethod]
    public Task TierPrice_ToModel()
    {
        var source = new TierPrice {
            Id = "tp-001",
            StoreId = "store-001",
            CustomerGroupId = "grp-001",
            Quantity = 10,
            Price = 89.99,
            StartDateTimeUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateTimeUtc = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            CurrencyCode = "USD"
        };
        return Verify(_mapper.Map<ProductModel.TierPriceModel>(source));
    }

    [TestMethod]
    public Task TierPriceModel_ToDomain()
    {
        var model = new ProductModel.TierPriceModel {
            StoreId = "store-001",
            CustomerGroupId = "grp-001",
            Quantity = 10,
            Price = 89.99,
            CurrencyCode = "USD"
        };
        return Verify(_mapper.Map<TierPrice>(model));
    }

    // ── ProductSpecificationAttribute ─────────────────────────────────────────

    [TestMethod]
    public Task ProductSpecification_ToModel()
    {
        var source = new ProductSpecificationAttribute {
            Id = "psa-001",
            AttributeTypeId = (SpecificationAttributeType)1,
            SpecificationAttributeId = "sa-001",
            SpecificationAttributeOptionId = "sao-001",
            CustomValue = "Custom",
            AllowFiltering = true,
            ShowOnProductPage = true,
            DisplayOrder = 2
        };
        return Verify(_mapper.Map<ProductModel.AddProductSpecificationAttributeModel>(source));
    }

    [TestMethod]
    public Task ProductSpecificationModel_ToDomain()
    {
        var model = new ProductModel.AddProductSpecificationAttributeModel {
            AttributeTypeId = (SpecificationAttributeType)1,
            SpecificationAttributeId = "sa-001",
            SpecificationAttributeOptionId = "sao-001",
            CustomValue = "Custom",
            AllowFiltering = true,
            ShowOnProductPage = true,
            DisplayOrder = 2
        };
        return Verify(_mapper.Map<ProductSpecificationAttribute>(model));
    }

    // ── ProductAttribute ──────────────────────────────────────────────────────

    [TestMethod]
    public Task ProductAttribute_ToModel()
    {
        var source = new ProductAttribute {
            Id = "pa-001",
            Name = "Color",
            Description = "Product color"
        };
        return Verify(_mapper.Map<ProductAttributeModel>(source));
    }

    [TestMethod]
    public Task ProductAttributeModel_ToDomain()
    {
        var model = new ProductAttributeModel {
            Name = "Color",
            Description = "Product color",
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<ProductAttribute>(model));
    }

    // ── PredefinedProductAttributeValue ───────────────────────────────────────

    [TestMethod]
    public Task PredefinedAttributeValue_ToModel()
    {
        var source = new PredefinedProductAttributeValue {
            Id = "ppav-001",
            Name = "Red",
            PriceAdjustment = 5.00,
            WeightAdjustment = 0.1,
            Cost = 1.5,
            IsPreSelected = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<PredefinedProductAttributeValueModel>(source));
    }

    [TestMethod]
    public Task PredefinedAttributeValueModel_ToDomain()
    {
        var model = new PredefinedProductAttributeValueModel {
            Name = "Red",
            PriceAdjustment = 5.00,
            WeightAdjustment = 0.1,
            Cost = 1.5,
            IsPreSelected = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<PredefinedProductAttributeValue>(model));
    }

    // ── ProductAttributeMapping ───────────────────────────────────────────────

    [TestMethod]
    public Task ProductAttributeMapping_ToModel()
    {
        var source = new ProductAttributeMapping {
            Id = "pam-001",
            ProductAttributeId = "pa-001",
            TextPrompt = "Choose color",
            IsRequired = true,
            AttributeControlTypeId = (AttributeControlType)1,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ProductModel.ProductAttributeMappingModel>(source));
    }

    [TestMethod]
    public Task ProductAttributeMappingModel_ToDomain()
    {
        var model = new ProductModel.ProductAttributeMappingModel {
            ProductAttributeId = "pa-001",
            TextPrompt = "Choose color",
            IsRequired = true,
            AttributeControlTypeId = (AttributeControlType)1,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ProductAttributeMapping>(model));
    }

    // ── ProductAttributeCombination ───────────────────────────────────────────

    [TestMethod]
    public Task ProductAttributeCombination_ToModel()
    {
        var source = new ProductAttributeCombination {
            Id = "pac-001",
            Sku = "SKU-RED-M",
            Gtin = "1234567890",
            AllowOutOfStockOrders = false,
            StockQuantity = 10,
            OverriddenPrice = 95.00,
            NotifyAdminForQuantityBelow = 2
        };
        return Verify(_mapper.Map<ProductAttributeCombinationModel>(source));
    }

    [TestMethod]
    public Task ProductAttributeCombinationModel_ToDomain()
    {
        var model = new ProductAttributeCombinationModel {
            Sku = "SKU-RED-M",
            Gtin = "1234567890",
            AllowOutOfStockOrders = false,
            StockQuantity = 10,
            OverriddenPrice = 95.00,
            NotifyAdminForQuantityBelow = 2
        };
        return Verify(_mapper.Map<ProductAttributeCombination>(model));
    }

    // ── PredefinedAttributeValue → ProductAttributeValue ─────────────────────

    [TestMethod]
    public Task PredefinedAttributeValue_ToProductAttributeValue()
    {
        var source = new PredefinedProductAttributeValue {
            Id = "ppav-001",
            Name = "Blue",
            PriceAdjustment = 3.00,
            WeightAdjustment = 0,
            Cost = 1.0,
            IsPreSelected = false,
            DisplayOrder = 2
        };
        return Verify(_mapper.Map<ProductAttributeValue>(source));
    }

    // ── ProductReview (model → domain) ────────────────────────────────────────

    [TestMethod]
    public Task ProductReviewModel_ToDomain()
    {
        var model = new ProductReviewModel {
            CustomerId = "cust-001",
            ProductId = "prod-001",
            IsApproved = true,
            Title = "Great product",
            ReviewText = "Really impressed!",
            ReplyText = "Thank you!",
            Rating = 5
        };
        return Verify(_mapper.Map<ProductReview>(model));
    }

    // ── SpecificationAttribute ────────────────────────────────────────────────

    [TestMethod]
    public Task SpecificationAttribute_ToModel()
    {
        var source = new SpecificationAttribute {
            Id = "sattr-001",
            Name = "Screen Size",
            DisplayOrder = 1,
            SeName = "screen-size"
        };
        return Verify(_mapper.Map<SpecificationAttributeModel>(source));
    }

    [TestMethod]
    public Task SpecificationAttributeModel_ToDomain()
    {
        var model = new SpecificationAttributeModel {
            Name = "Screen Size",
            DisplayOrder = 1,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<SpecificationAttribute>(model));
    }

    [TestMethod]
    public Task SpecificationAttributeOption_ToModel()
    {
        var source = new SpecificationAttributeOption {
            Id = "sao-001",
            Name = "15 inch",
            DisplayOrder = 1,
            ColorSquaresRgb = "#FFFFFF",
            SeName = "15-inch"
        };
        return Verify(_mapper.Map<SpecificationAttributeOptionModel>(source));
    }

    [TestMethod]
    public Task SpecificationAttributeOptionModel_ToDomain()
    {
        var model = new SpecificationAttributeOptionModel {
            Name = "15 inch",
            DisplayOrder = 1,
            ColorSquaresRgb = "#FFFFFF"
        };
        return Verify(_mapper.Map<SpecificationAttributeOption>(model));
    }
}
