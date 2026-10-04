using Grand.Business.Catalog.Services.Categories;
using Grand.Business.Common.Services.Security;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Categories;

/// <summary>
///     The tree is assembled outside the cache, one level per entry. These tests pin the two
///     properties that depends on: the order callers already relied on, and the fact that the
///     assembled list is the caller's own rather than a shared cache entry.
/// </summary>
[TestClass]
public class CategoryServiceTreeTests
{
    private CategoryService _categoryService;
    private IRepository<Category> _categoryRepository;

    [TestInitialize]
    public void InitializeTests()
    {
        _categoryRepository = new MongoDBRepositoryTest<Category>();
        //one customer for the whole fixture: a fresh instance per access would give every call its
        //own id, hence its own cache key, and the caching under test would never be exercised
        var customer = new Customer { Id = "customer" };
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "store" });
        contextAccessor.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => customer);
        var mediator = new Mock<IMediator>();
        var cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), mediator.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _categoryService = new CategoryService(cacheBase, _categoryRepository, contextAccessor.Object,
            mediator.Object, new AclService(new AccessControlConfig()), new AccessControlConfig());
    }

    /// <summary>
    ///     root -> a -> a1, root -> b -> b1
    /// </summary>
    private async Task GivenATwoLevelTree()
    {
        await _categoryService.InsertCategory(new Category { Id = "a", ParentCategoryId = "root", Published = true, DisplayOrder = 1 });
        await _categoryService.InsertCategory(new Category { Id = "b", ParentCategoryId = "root", Published = true, DisplayOrder = 2 });
        await _categoryService.InsertCategory(new Category { Id = "a1", ParentCategoryId = "a", Published = true });
        await _categoryService.InsertCategory(new Category { Id = "b1", ParentCategoryId = "b", Published = true });
    }

    [TestMethod]
    public async Task AllLevels_KeepsTheWholeLevelBeforeEachSubtree()
    {
        await GivenATwoLevelTree();

        var result = await _categoryService.GetAllCategoriesByParentCategoryId("root", includeAllLevels: true);

        CollectionAssert.AreEqual(new[] { "a", "b", "a1", "b1" }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task WithoutAllLevels_ReturnsOnlyTheDirectChildren()
    {
        await GivenATwoLevelTree();

        var result = await _categoryService.GetAllCategoriesByParentCategoryId("root");

        CollectionAssert.AreEqual(new[] { "a", "b" }, result.Select(x => x.Id).ToArray());
    }

    /// <summary>
    ///     The assembled list is built per call, so a caller that mutates it cannot corrupt what the
    ///     next caller sees.
    /// </summary>
    [TestMethod]
    public async Task AllLevels_HandsBackAListTheCallerOwns()
    {
        await GivenATwoLevelTree();

        var first = await _categoryService.GetAllCategoriesByParentCategoryId("root", includeAllLevels: true);
        first.Clear();
        var second = await _categoryService.GetAllCategoriesByParentCategoryId("root", includeAllLevels: true);

        Assert.HasCount(4, second);
    }
}
