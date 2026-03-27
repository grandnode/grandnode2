using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Domain.Pages;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Layouts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class CatalogLayoutMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<CategoryLayoutProfile>();
            cfg.AddProfile<BrandLayoutProfile>();
            cfg.AddProfile<CollectionLayoutProfile>();
            cfg.AddProfile<ProductLayoutProfile>();
            cfg.AddProfile<PageLayoutProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── CategoryLayout ────────────────────────────────────────────────────────

    [TestMethod]
    public Task CategoryLayout_ToModel()
    {
        var source = new CategoryLayout { Id = "cl-001", Name = "DefaultLayout", ViewPath = "CategoryTemplate.Simple", DisplayOrder = 1 };
        return Verify(_mapper.Map<CategoryLayoutModel>(source));
    }

    [TestMethod]
    public Task CategoryLayoutModel_ToDomain()
    {
        var model = new CategoryLayoutModel { Name = "DefaultLayout", ViewPath = "CategoryTemplate.Simple", DisplayOrder = 1 };
        return Verify(_mapper.Map<CategoryLayout>(model));
    }

    // ── BrandLayout ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task BrandLayout_ToModel()
    {
        var source = new BrandLayout { Id = "bl-001", Name = "BrandDefault", ViewPath = "BrandTemplate.Simple", DisplayOrder = 2 };
        return Verify(_mapper.Map<BrandLayoutModel>(source));
    }

    [TestMethod]
    public Task BrandLayoutModel_ToDomain()
    {
        var model = new BrandLayoutModel { Name = "BrandDefault", ViewPath = "BrandTemplate.Simple", DisplayOrder = 2 };
        return Verify(_mapper.Map<BrandLayout>(model));
    }

    // ── CollectionLayout ──────────────────────────────────────────────────────

    [TestMethod]
    public Task CollectionLayout_ToModel()
    {
        var source = new CollectionLayout { Id = "cll-001", Name = "CollectionDefault", ViewPath = "CollectionTemplate.Simple", DisplayOrder = 1 };
        return Verify(_mapper.Map<CollectionLayoutModel>(source));
    }

    [TestMethod]
    public Task CollectionLayoutModel_ToDomain()
    {
        var model = new CollectionLayoutModel { Name = "CollectionDefault", ViewPath = "CollectionTemplate.Simple", DisplayOrder = 1 };
        return Verify(_mapper.Map<CollectionLayout>(model));
    }

    // ── ProductLayout ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task ProductLayout_ToModel()
    {
        var source = new ProductLayout { Id = "pl-001", Name = "SimpleProduct", ViewPath = "ProductTemplate.Simple", DisplayOrder = 5 };
        return Verify(_mapper.Map<ProductLayoutModel>(source));
    }

    [TestMethod]
    public Task ProductLayoutModel_ToDomain()
    {
        var model = new ProductLayoutModel { Name = "SimpleProduct", ViewPath = "ProductTemplate.Simple", DisplayOrder = 5 };
        return Verify(_mapper.Map<ProductLayout>(model));
    }

    // ── PageLayout ────────────────────────────────────────────────────────────

    [TestMethod]
    public Task PageLayout_ToModel()
    {
        var source = new PageLayout { Id = "pgl-001", Name = "PageDefault", ViewPath = "PageTemplate.Default", DisplayOrder = 1 };
        return Verify(_mapper.Map<PageLayoutModel>(source));
    }

    [TestMethod]
    public Task PageLayoutModel_ToDomain()
    {
        var model = new PageLayoutModel { Name = "PageDefault", ViewPath = "PageTemplate.Default", DisplayOrder = 1 };
        return Verify(_mapper.Map<PageLayout>(model));
    }
}
