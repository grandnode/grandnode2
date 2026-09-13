using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Services.Security;
using Grand.Data;
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

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductServiceBatchTests
{
    private readonly List<Product> _products = [
        new() { Id = "1", Published = true, VisibleIndividually = true },
        new() { Id = "2", Published = true, VisibleIndividually = true },
        new() { Id = "3", Published = true, VisibleIndividually = true }
    ];

    private MemoryCacheBase _cacheBase;
    private ProductService _productService;
    private List<string[]> _queries;

    [TestInitialize]
    public void InitializeTests()
    {
        _queries = [];
        var repository = new Mock<IRepository<Product>>();
        repository.Setup(x => x.Table).Returns(() => _products.AsQueryable());
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Product> query, CancellationToken _) =>
            {
                var result = query.ToList();
                _queries.Add(result.Select(x => x.Id).ToArray());
                return result;
            });

        var customer = new Customer { Id = "customer" };
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "store" });
        contextAccessor.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => customer);
        var mediator = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), mediator.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productService = new ProductService(_cacheBase, repository.Object, contextAccessor.Object,
            mediator.Object, new AclService(new AccessControlConfig()));
    }

    [TestMethod]
    public async Task ColdCache_ReadsEveryProductInOneQuery()
    {
        var result = await _productService.GetProductsByIds(["1", "2", "3"], true);

        Assert.HasCount(3, result);
        Assert.HasCount(1, _queries);
    }

    [TestMethod]
    public async Task WarmCache_DoesNotQuery()
    {
        await _productService.GetProductsByIds(["1", "2", "3"], true);

        var result = await _productService.GetProductsByIds(["1", "2", "3"], true);

        Assert.HasCount(3, result);
        Assert.HasCount(1, _queries);
    }

    [TestMethod]
    public async Task PartlyWarmCache_QueriesOnlyTheMissingProducts()
    {
        await _productService.GetProductsByIds(["1"], true);

        var result = await _productService.GetProductsByIds(["1", "2", "3"], true);

        Assert.HasCount(3, result);
        Assert.HasCount(2, _queries);
        CollectionAssert.AreEquivalent(new[] { "2", "3" }, _queries[1]);
    }

    [TestMethod]
    public async Task MissingProduct_IsCachedAndNotQueriedAgain()
    {
        await _productService.GetProductsByIds(["missing"], true);

        var result = await _productService.GetProductsByIds(["missing"], true);

        Assert.IsEmpty(result);
        Assert.HasCount(1, _queries);
    }

    [TestMethod]
    public async Task SharesCacheEntriesWithGetProductById()
    {
        await _productService.GetProductsByIds(["1"], true);
        _products.Clear();

        Assert.IsNotNull(await _productService.GetProductById("1"));
    }

    [TestMethod]
    public async Task KeepsTheOrderOfTheIdentifiersGiven()
    {
        var result = await _productService.GetProductsByIds(["3", "1", "2"], true);

        CollectionAssert.AreEqual(new[] { "3", "1", "2" }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task SkipsAnIdentifierThatMatchesNothing()
    {
        var result = await _productService.GetProductsByIds(["1", "missing", "2"], true);

        CollectionAssert.AreEqual(new[] { "1", "2" }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task RepeatsAProductWhoseIdentifierRepeats()
    {
        var result = await _productService.GetProductsByIds(["1", "1"], true);

        CollectionAssert.AreEqual(new[] { "1", "1" }, result.Select(x => x.Id).ToArray());
        Assert.HasCount(1, _queries);
    }

    [TestMethod]
    public async Task ReturnsNothingForAnEmptyRequest()
    {
        Assert.IsEmpty(await _productService.GetProductsByIds([], true));
        Assert.IsEmpty(_queries);
    }
}
