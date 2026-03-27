using Grand.Mapping;
using Grand.Business.Catalog.Services.ExportImport.Mapper;
using Grand.Business.Core.Dto;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.Catalog;

/// <summary>
/// Tests for ExportImport mapping profiles.
/// NOTE: These profiles set UpdatedOnUtc = DateTime.UtcNow via MapFrom.
/// Verify automatically scrubs DateTime values, so snapshots remain stable.
/// </summary>
[TestClass]
public class ExportImportMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<BrandProfile>();
            cfg.AddProfile<CollectionProfile>();
            cfg.AddProfile<ProductProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── Category ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task CategoryDto_ToEntity_WithNullableValues()
    {
        var dto = new CategoryDto {
            Id = "cat-export-1",
            Name = "Electronics",
            SeName = "electronics",
            Description = "Electronic products",
            MetaTitle = "Electronics",
            MetaKeywords = "electronics, gadgets",
            MetaDescription = "Best electronics",
            CategoryLayoutId = "layout-1",
            ParentCategoryId = "",
            PageSize = 12,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24",
            ShowOnHomePage = true,
            FeaturedProductsOnHomePage = false,
            ShowOnSearchBox = false,
            SearchBoxDisplayOrder = 0,
            IncludeInMenu = true,
            DefaultSort = 0,
            HideOnCatalog = false,
            Published = true,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<Category>(dto));
    }

    [TestMethod]
    public Task CategoryDto_ToEntity_NullConditions_NotApplied()
    {
        // When nullable fields are null, the mapping condition is false, so
        // the destination keeps its default values for those members.
        var dto = new CategoryDto {
            Name = "Partial Category"
            // All nullable properties are null → conditions false → not mapped
        };
        return Verify(_mapper.Map<Category>(dto));
    }

    // ── Brand ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task BrandDto_ToEntity_WithNullableValues()
    {
        var dto = new BrandDto {
            Id = "brand-export-1",
            Name = "Nike",
            SeName = "nike",
            Description = "Sports brand",
            MetaTitle = "Nike",
            BrandLayoutId = "layout-1",
            PageSize = 12,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24",
            ShowOnHomePage = true,
            IncludeInMenu = true,
            DefaultSort = 0,
            Published = true,
            DisplayOrder = 1,
            ExternalId = "ext-001"
        };
        return Verify(_mapper.Map<Brand>(dto));
    }

    [TestMethod]
    public Task BrandDto_ToEntity_NullConditions_NotApplied()
    {
        var dto = new BrandDto {
            Name = "Partial Brand"
        };
        return Verify(_mapper.Map<Brand>(dto));
    }

    // ── Collection ────────────────────────────────────────────────────────────

    [TestMethod]
    public Task CollectionDto_ToEntity_WithNullableValues()
    {
        var dto = new CollectionDto {
            Id = "col-export-1",
            Name = "Summer 2025",
            SeName = "summer-2025",
            Description = "Summer items",
            MetaTitle = "Summer 2025",
            CollectionLayoutId = "layout-1",
            PageSize = 12,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 12, 24",
            ShowOnHomePage = true,
            FeaturedProductsOnHomePage = false,
            IncludeInMenu = true,
            DefaultSort = 0,
            Published = true,
            DisplayOrder = 3,
            ExternalId = "ext-col-001"
        };
        return Verify(_mapper.Map<Collection>(dto));
    }

    [TestMethod]
    public Task CollectionDto_ToEntity_NullConditions_NotApplied()
    {
        var dto = new CollectionDto {
            Name = "Partial Collection"
        };
        return Verify(_mapper.Map<Collection>(dto));
    }

    // ── Product ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task ProductDto_ToEntity_BasicProperties()
    {
        // ProductProfile has many nullable conditions; test with a minimal set
        var dto = new Grand.Business.Core.Dto.ProductDto {
            Id = "prod-export-1",
            Name = "Test Product",
            ShortDescription = "Short",
            FullDescription = "Full description",
            Sku = "SKU001",
            Published = true,
            VisibleIndividually = true,
            ShowOnHomePage = false,
            Price = 29.99,
            StockQuantity = 100,
            DisplayOrder = 0
        };
        return Verify(_mapper.Map<Product>(dto));
    }

    [TestMethod]
    public Task ProductDto_ToEntity_ExistingIdPreservedWhenEmpty()
    {
        // When Id is null/empty, the Condition prevents overwriting the existing Id.
        var dto = new Grand.Business.Core.Dto.ProductDto {
            Id = "",     // empty → condition false → Id not overwritten
            Name = "No Id Product"
        };
        var destination = new Product { Id = "existing-id" };
        return Verify(_mapper.Map(dto, destination));
    }
}
