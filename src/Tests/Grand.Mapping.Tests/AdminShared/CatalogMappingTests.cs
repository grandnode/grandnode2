using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Models.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class CatalogMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<BrandProfile>();
            cfg.AddProfile<CollectionProfile>();
            cfg.AddProfile<DeliveryDateProfile>();
            cfg.AddProfile<DiscountProfile>();
            cfg.AddProfile<GiftVoucherProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── Category ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Category_ToModel()
    {
        var source = new Category {
            Id = "cat-001",
            Name = "Electronics",
            Description = "Electronic products",
            BottomDescription = "More electronics",
            CategoryLayoutId = "layout-001",
            MetaKeywords = "electronics, gadgets",
            MetaDescription = "Shop electronics online",
            MetaTitle = "Electronics",
            ParentCategoryId = "parent-001",
            PictureId = "pic-001",
            PageSize = 15,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "15, 30, 60",
            ShowOnHomePage = true,
            FeaturedProductsOnHomePage = false,
            ShowOnSearchBox = true,
            SearchBoxDisplayOrder = 2,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = 5,
            ExternalId = "ext-001",
            Flag = "New",
            FlagStyle = "bg-success",
            Icon = "fa fa-laptop",
            HideOnCatalog = false,
            DefaultSort = 0,
            SeName = "electronics"
        };
        var result = _mapper.Map<CategoryModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task CategoryModel_ToDomain()
    {
        var model = new CategoryModel {
            Id = "cat-001",
            Name = "Electronics",
            Description = "Electronic products",
            BottomDescription = "More electronics",
            CategoryLayoutId = "layout-001",
            MetaKeywords = "electronics, gadgets",
            MetaDescription = "Shop electronics online",
            MetaTitle = "Electronics",
            ParentCategoryId = "parent-001",
            PictureId = "pic-001",
            PageSize = 15,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "15, 30, 60",
            ShowOnHomePage = true,
            FeaturedProductsOnHomePage = false,
            ShowOnSearchBox = true,
            SearchBoxDisplayOrder = 2,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = 5,
            ExternalId = "ext-001",
            Flag = "New",
            FlagStyle = "bg-success",
            Icon = "fa fa-laptop",
            HideOnCatalog = false,
            DefaultSort = 0,
            CustomerGroups = ["grp-001", "grp-002"],
            Stores = ["store-001"]
        };
        var result = _mapper.Map<Category>(model);
        return Verify(result);
    }

    // ── Brand ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Brand_ToModel()
    {
        var source = new Brand {
            Id = "brand-001",
            Name = "Apple",
            Description = "Apple brand",
            BottomDescription = "Bottom",
            BrandLayoutId = "layout-001",
            MetaKeywords = "apple",
            MetaDescription = "Apple products",
            MetaTitle = "Apple",
            PictureId = "pic-001",
            PageSize = 20,
            AllowCustomersToSelectPageSize = false,
            PageSizeOptions = "20, 40",
            ShowOnHomePage = true,
            IncludeInMenu = true,
            Icon = "fa fa-apple",
            DefaultSort = -1,
            Published = true,
            DisplayOrder = 1,
            ExternalId = "ext-brand-001",
            SeName = "apple"
        };
        var result = _mapper.Map<BrandModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task BrandModel_ToDomain()
    {
        var model = new BrandModel {
            Id = "brand-001",
            Name = "Apple",
            Description = "Apple brand",
            BottomDescription = "Bottom",
            BrandLayoutId = "layout-001",
            MetaKeywords = "apple",
            MetaDescription = "Apple products",
            MetaTitle = "Apple",
            PictureId = "pic-001",
            PageSize = 20,
            AllowCustomersToSelectPageSize = false,
            PageSizeOptions = "20, 40",
            ShowOnHomePage = true,
            IncludeInMenu = true,
            Icon = "fa fa-apple",
            DefaultSort = -1,
            Published = true,
            DisplayOrder = 1,
            ExternalId = "ext-brand-001",
            CustomerGroups = ["grp-001"],
            Stores = ["store-001", "store-002"]
        };
        var result = _mapper.Map<Brand>(model);
        return Verify(result);
    }

    // ── Collection ────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Collection_ToModel()
    {
        var source = new Collection {
            Id = "col-001",
            Name = "Summer Sale",
            Description = "Summer collection",
            BottomDescription = "End of summer",
            CollectionLayoutId = "layout-002",
            MetaKeywords = "summer, sale",
            MetaDescription = "Summer sales",
            MetaTitle = "Summer Collection",
            PictureId = "pic-002",
            PageSize = 10,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "10, 20",
            ShowOnHomePage = false,
            IncludeInMenu = false,
            Published = true,
            DisplayOrder = 3,
            ExternalId = "ext-col-001",
            SeName = "summer-sale"
        };
        var result = _mapper.Map<CollectionModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task CollectionModel_ToDomain()
    {
        var model = new CollectionModel {
            Id = "col-001",
            Name = "Summer Sale",
            Description = "Summer collection",
            CollectionLayoutId = "layout-002",
            Published = true,
            DisplayOrder = 3,
            Stores = ["store-001"]
        };
        var result = _mapper.Map<Collection>(model);
        return Verify(result);
    }

    // ── DeliveryDate ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task DeliveryDate_ToModel()
    {
        var source = new DeliveryDate {
            Id = "dd-001",
            Name = "2-3 Business Days",
            DisplayOrder = 1,
            ColorSquaresRgb = "#00FF00"
        };
        var result = _mapper.Map<DeliveryDateModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task DeliveryDateModel_ToDomain()
    {
        var model = new DeliveryDateModel {
            Name = "2-3 Business Days",
            DisplayOrder = 1,
            ColorSquaresRgb = "#00FF00"
        };
        var result = _mapper.Map<DeliveryDate>(model);
        return Verify(result);
    }

    // ── Discount ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Discount_ToModel()
    {
        var source = new Discount {
            Id = "disc-001",
            Name = "10% Off",
            DiscountTypeId = (DiscountType)1,
            UsePercentage = true,
            DiscountPercentage = 10,
            DiscountAmount = 0,
            MaximumDiscountAmount = 100,
            StartDateUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            RequiresCouponCode = true,
            IsEnabled = true,
            LimitationTimes = 100,
            MaximumDiscountedQuantity = 5,
            CurrencyCode = "USD",
            Stores = ["store-001"]
        };
        var result = _mapper.Map<DiscountModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task Discount_ToModel_NullableFields_WithValues()
    {
        var source = new Discount {
            Id = "disc-002",
            Name = "Summer Sale",
            DiscountTypeId = (DiscountType)1,
            MaximumDiscountAmount = 50.0,
            StartDateUtc = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2024, 8, 31, 23, 59, 59, DateTimeKind.Utc),
            IsEnabled = true
        };
        var result = _mapper.Map<DiscountModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task Discount_ToModel_NullableFields_WithNull()
    {
        // Regression: Nullable<T> → T coercion threw InvalidOperationException
        // when the source nullable was null (e.g. MaximumDiscountAmount, StartDateUtc).
        var source = new Discount {
            Id = "disc-003",
            Name = "No Dates Discount",
            DiscountTypeId = (DiscountType)1,
            // MaximumDiscountAmount = null  (double?)
            // StartDateUtc          = null  (DateTime?)
            // EndDateUtc            = null  (DateTime?)
            IsEnabled = true
        };
        var result = _mapper.Map<DiscountModel>(source);
        return Verify(result);
    }

    [TestMethod]
    public Task DiscountModel_ToDomain()
    {
        var model = new DiscountModel {
            Id = "disc-001",
            Name = "10% Off",
            DiscountTypeId = 1,
            UsePercentage = true,
            DiscountPercentage = 10,
            DiscountAmount = 0,
            RequiresCouponCode = true,
            IsEnabled = true,
            Stores = ["store-001", "store-002"]
        };
        var result = _mapper.Map<Discount>(model);
        return Verify(result);
    }

    // ── GiftVoucher ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task GiftVoucher_ToModel()
    {
        var source = new GiftVoucher {
            Id = "gv-001",
            GiftVoucherTypeId = 0,
            Amount = 50.00,
            IsGiftVoucherActivated = true,
            Code = "GIFT50",
            RecipientName = "John Doe",
            RecipientEmail = "john@example.com",
            SenderName = "Jane",
            SenderEmail = "jane@example.com",
            Message = "Happy Birthday!",
            IsRecipientNotified = false,
            CreatedOnUtc = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            CurrencyCode = "USD"
        };
        var result = _mapper.Map<GiftVoucherModel>(source);
        return Verify(result);
    }
}
