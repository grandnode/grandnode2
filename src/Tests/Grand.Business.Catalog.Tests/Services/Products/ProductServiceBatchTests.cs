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

/// <summary>
///     GetProductsByIds used to loop GetProductById, so a "batch" read cost one query per identifier.
///     These tests count reads at the repository, because the number of round trips is the whole point
///     of the change - asserting only on the returned products would pass either way.
/// </summary>
[TestClass]
public class ProductServiceBatchTests
{
    private MemoryCacheBase _cacheBase;
    private ProductService _productService;
    private Mock<IRepository<Product>> _repository;
    private int _tableReads;

    [TestInitialize]
    public void InitializeTests()
    {
        var products = new List<Product> {
            new() { Id = "1", Published = true, VisibleIndividually = true },
            new() { Id = "2", Published = true, VisibleIndividually = true },
            new() { Id = "3", Published = true, VisibleIndividually = true }
        };

        _tableReads = 0;
        _repository = new Mock<IRepository<Product>>();
        _repository.Setup(x => x.Table).Returns(() =>
        {
            _tableReads++;
            return products.AsQueryable();
        });

        //a single customer and store: a fresh instance per access would give each call its own cache key
        var customer = new Customer { Id = "customer" };
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "store" });
        contextAccessor.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => customer);
        var mediator = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), mediator.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productService = new ProductService(_cacheBase, _repository.Object, contextAccessor.Object,
            mediator.Object, new AclService(new AccessControlConfig()));
    }

    [TestMethod]
    public async Task ColdCache_ReadsEveryProductInOneGo()
    {
        var result = await _productService.GetProductsByIds(["1", "2", "3"], true);

        Assert.HasCount(3, result);
        Assert.AreEqual(1, _tableReads, "three identifiers must not cost three reads");
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task WarmCache_DoesNotReadAtAll()
    {
        await _productService.GetProductsByIds(["1", "2", "3"], true);
        var reads = _tableReads;

        var result = await _productService.GetProductsByIds(["1", "2", "3"], true);

        Assert.HasCount(3, result);
        Assert.AreEqual(reads, _tableReads, "everything was already cached");
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
    }

    [TestMethod]
    public async Task ReturnsNothingForAnEmptyRequest()
    {
        Assert.IsEmpty(await _productService.GetProductsByIds([], true));
        Assert.AreEqual(0, _tableReads);
    }
}
