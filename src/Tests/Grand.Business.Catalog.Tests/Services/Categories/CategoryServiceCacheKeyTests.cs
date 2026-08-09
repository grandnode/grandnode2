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
///     The category tree a visitor may see depends on the customer's groups, never on which customer
///     they are - the ACL filter inside the service reads GetCustomerGroupIds(). Keying the cache by
///     customer identity therefore gives every guest a private copy of the same data.
///     Reference equality is the assertion here: the memory cache hands back the very instance it
///     stored, so sharing an instance proves the two lookups produced the same key.
/// </summary>
[TestClass]
public class CategoryServiceCacheKeyTests
{
    private MemoryCacheBase _cacheBase;
    private IRepository<Category> _categoryRepository;
    private CategoryService _categoryService;
    private Customer _currentCustomer;

    [TestInitialize]
    public void InitializeTests()
    {
        _categoryRepository = new MongoDBRepositoryTest<Category>();
        _currentCustomer = new Customer();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "store" });
        contextAccessor.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => _currentCustomer);
        var mediator = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), mediator.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _categoryService = new CategoryService(_cacheBase, _categoryRepository, contextAccessor.Object,
            mediator.Object, new AclService(new AccessControlConfig()), new AccessControlConfig());
    }

    private static Customer WithGroups(string id, params string[] groups)
    {
        var customer = new Customer { Id = id };
        foreach (var group in groups)
            customer.Groups.Add(group);
        return customer;
    }

    [TestMethod]
    public async Task TwoCustomersOfTheSameGroup_ShareTheCachedTree()
    {
        await _categoryService.InsertCategory(new Category { ParentCategoryId = "root", Published = true });

        _currentCustomer = WithGroups("customer-1", "group-a");
        var first = await _categoryService.GetAllCategoriesByParentCategoryId("root");

        _currentCustomer = WithGroups("customer-2", "group-a");
        var second = await _categoryService.GetAllCategoriesByParentCategoryId("root");

        Assert.AreSame(first, second, "same groups must resolve to the same cache entry");
    }

    [TestMethod]
    public async Task CustomersOfDifferentGroups_DoNotShareTheCachedTree()
    {
        await _categoryService.InsertCategory(new Category { ParentCategoryId = "root", Published = true });

        _currentCustomer = WithGroups("customer-1", "group-a");
        var first = await _categoryService.GetAllCategoriesByParentCategoryId("root");

        _currentCustomer = WithGroups("customer-2", "group-b");
        var second = await _categoryService.GetAllCategoriesByParentCategoryId("root");

        Assert.AreNotSame(first, second, "the ACL filter differs per group, so the entries must differ");
    }
}
