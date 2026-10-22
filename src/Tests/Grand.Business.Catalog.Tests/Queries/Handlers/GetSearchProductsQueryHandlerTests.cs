using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetSearchProductsQueryHandlerTests
{
    private ICacheBase _cache;
    private IRepository<Product> _repository;
    private GetSearchProductsQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _cache = Cache();
        handler = new GetSearchProductsQueryHandler(_repository, new Mock<ISpecificationAttributeService>().Object, _cache,
            new CatalogSettings { IgnoreFilterableSpecAttributeOption = true }, new AccessControlConfig(),
            new DatabaseConfig(), Mock.Of<IDatabaseContext>(),
            NullLogger<GetSearchProductsQueryHandler>.Instance);
    }


    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        var searchProductsQuery = new GetSearchProductsQuery {
            Customer = new Customer()
        };
        //Act
        var result = await handler.Handle(searchProductsQuery, CancellationToken.None);
        //Arrange
        Assert.IsNotNull(result.products);
    }

    [TestMethod]
    public async Task Handle_WhenCustomerGroupIdsGivenWithoutCustomer_FiltersByThoseGroups()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Id = "open", Published = true, VisibleIndividually = true });
        await _repository.InsertAsync(new Product {
            Id = "guests", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = ["guests-group"]
        });
        await _repository.InsertAsync(new Product {
            Id = "admins", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = ["admins-group"]
        });
        var searchProductsQuery = new GetSearchProductsQuery {
            CustomerGroupIds = ["guests-group"]
        };
        //Act
        var result = await handler.Handle(searchProductsQuery, CancellationToken.None);
        //Assert
        CollectionAssert.AreEquivalent(new[] { "open", "guests" }, result.products.Select(p => p.Id).ToList());
    }

    [TestMethod]
    public async Task Handle_WhenShowHiddenWithoutCustomer_DoesNotFilterByGroups()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Id = "open", Published = true, VisibleIndividually = true });
        await _repository.InsertAsync(new Product {
            Id = "admins", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = ["admins-group"]
        });
        var searchProductsQuery = new GetSearchProductsQuery { ShowHidden = true };
        //Act
        var result = await handler.Handle(searchProductsQuery, CancellationToken.None);
        //Assert
        CollectionAssert.AreEquivalent(new[] { "open", "admins" }, result.products.Select(p => p.Id).ToList());
    }

    [TestMethod]
    public async Task Handle_WhenRequestCancelled_StopsTheProductQuery()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        var searchProductsQuery = new GetSearchProductsQuery { Customer = new Customer() };
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.Handle(searchProductsQuery, cancellation.Token));
    }

    [TestMethod]
    public async Task Handle_WhenFilterableSpecificationsRequested_ReturnsDistinctFilterableOptionIdsOfMatchingProducts()
    {
        //Arrange
        var filteringHandler = new GetSearchProductsQueryHandler(_repository,
            new Mock<ISpecificationAttributeService>().Object, Cache(),
            new CatalogSettings { IgnoreFilterableSpecAttributeOption = false }, new AccessControlConfig(),
            new DatabaseConfig(), Mock.Of<IDatabaseContext>(),
            NullLogger<GetSearchProductsQueryHandler>.Instance);
        await _repository.InsertAsync(new Product {
            Published = true, VisibleIndividually = true,
            ProductSpecificationAttributes = {
                new ProductSpecificationAttribute { SpecificationAttributeOptionId = "red", AllowFiltering = true },
                new ProductSpecificationAttribute { SpecificationAttributeOptionId = "cotton", AllowFiltering = false }
            }
        });
        await _repository.InsertAsync(new Product {
            Published = true, VisibleIndividually = true,
            ProductSpecificationAttributes = {
                new ProductSpecificationAttribute { SpecificationAttributeOptionId = "red", AllowFiltering = true },
                new ProductSpecificationAttribute { SpecificationAttributeOptionId = "large", AllowFiltering = true }
            }
        });
        await _repository.InsertAsync(new Product {
            Published = false, VisibleIndividually = true,
            ProductSpecificationAttributes = {
                new ProductSpecificationAttribute { SpecificationAttributeOptionId = "blue", AllowFiltering = true }
            }
        });
        var searchProductsQuery = new GetSearchProductsQuery {
            Customer = new Customer(), LoadFilterableSpecificationAttributeOptionIds = true
        };
        //Act
        var result = await filteringHandler.Handle(searchProductsQuery, CancellationToken.None);
        //Assert
        CollectionAssert.AreEquivalent(new[] { "red", "large" },
            result.filterableSpecificationAttributeOptionIds.ToList());
    }

    [TestMethod]
    public async Task Handle_WhenFilterableSpecificationsRequested_StartsBothQueriesBeforeAwaitingEither()
    {
        //Arrange
        //each query completes only once the other one was started, so a sequential handler times out
        var pageStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var optionsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<IRepository<Product>>();
        repository.Setup(x => x.Table).Returns(new List<Product>().AsQueryable());
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => {
                pageStarted.TrySetResult();
                await optionsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return new List<Product> { new() { Id = "1" } };
            });
        repository.Setup(x => x.CountAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => {
                optionsStarted.TrySetResult();
                await pageStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return new List<string> { "red" };
            });
        var filteringHandler = new GetSearchProductsQueryHandler(repository.Object,
            new Mock<ISpecificationAttributeService>().Object, Cache(),
            new CatalogSettings { IgnoreFilterableSpecAttributeOption = false }, new AccessControlConfig(),
            new DatabaseConfig(), Mock.Of<IDatabaseContext>(),
            NullLogger<GetSearchProductsQueryHandler>.Instance);
        var searchProductsQuery = new GetSearchProductsQuery {
            Customer = new Customer(), LoadFilterableSpecificationAttributeOptionIds = true
        };
        //Act
        var result = await filteringHandler.Handle(searchProductsQuery, CancellationToken.None);
        //Assert
        Assert.AreEqual("1", result.products.Single().Id);
        CollectionAssert.AreEqual(new[] { "red" }, result.filterableSpecificationAttributeOptionIds.ToList());
    }

    [TestMethod]
    public async Task Handle_WhenPageQueryFails_CancelsTheFilterableQueryAndRethrows()
    {
        //Arrange
        var optionsToken = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<IRepository<Product>>();
        repository.Setup(x => x.Table).Returns(new List<Product>().AsQueryable());
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => {
                //fail once the options query is running
                await optionsToken.Task.WaitAsync(TimeSpan.FromSeconds(5));
                throw new InvalidOperationException("page failed");
            });
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IQueryable<string> _, CancellationToken token) => {
                optionsToken.TrySetResult(token);
                //never finishes on its own - only the cancellation ends it
                await Task.Delay(Timeout.Infinite, token);
                return new List<string>();
            });
        var filteringHandler = FilteringHandler(repository.Object, new DatabaseConfig(), Mock.Of<IDatabaseContext>());
        var searchProductsQuery = new GetSearchProductsQuery {
            Customer = new Customer(), LoadFilterableSpecificationAttributeOptionIds = true
        };
        //Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filteringHandler.Handle(searchProductsQuery, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
        //Assert
        Assert.AreEqual("page failed", exception.Message);
        Assert.IsTrue((await optionsToken.Task).IsCancellationRequested);
    }

    [TestMethod]
    public async Task Handle_WhenRequestCancelledAndOtherQueryFaults_ThrowsCancelledWithoutTheScanFallback()
    {
        //Arrange
        var started = 0;
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IRepository<Product>>();
        repository.Setup(x => x.Table).Returns(new List<Product>().AsQueryable());
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IQueryable<Product> _, CancellationToken token) => {
                if (Interlocked.Increment(ref started) == 2) await cancellation.CancelAsync();
                await Task.Delay(Timeout.Infinite, token);
                return new List<Product>();
            });
        repository.Setup(x => x.ToListAsync(It.IsAny<IQueryable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IQueryable<string> _, CancellationToken token) => {
                if (Interlocked.Increment(ref started) == 2) await cancellation.CancelAsync();
                //a driver reports the torn-down connection as an ordinary fault
                await Task.Delay(Timeout.Infinite, token).ContinueWith(_ => { }, TaskScheduler.Default);
                throw new InvalidOperationException("connection closed");
            });
        var databaseContext = new Mock<IDatabaseContext>();
        databaseContext.Setup(x => x.TextSearch(It.IsAny<IRepository<Product>>(), It.IsAny<string>()))
            .Returns(new List<Product>().AsQueryable());
        var filteringHandler = FilteringHandler(repository.Object, new DatabaseConfig { UseProductTextSearch = true },
            databaseContext.Object);
        var searchProductsQuery = new GetSearchProductsQuery {
            Customer = new Customer(), Keywords = "phone", LoadFilterableSpecificationAttributeOptionIds = true
        };
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            filteringHandler.Handle(searchProductsQuery, cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)));
        repository.Verify(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_StorefrontListingWithFullPage_ReusesTheCachedTotal()
    {
        //Arrange
        await InsertVisibleProducts(3);
        await handler.Handle(Listing(pageSize: 2), CancellationToken.None);
        await InsertVisibleProducts(1);
        //Act
        var result = await handler.Handle(Listing(pageSize: 2), CancellationToken.None);
        //Assert
        Assert.AreEqual(3, result.products.TotalCount);
    }

    [TestMethod]
    public async Task Handle_StorefrontListingWithShortPage_ReturnsTheExactTotal()
    {
        //Arrange
        await InsertVisibleProducts(3);
        await handler.Handle(Listing(pageSize: 10), CancellationToken.None);
        await InsertVisibleProducts(1);
        //Act
        var result = await handler.Handle(Listing(pageSize: 10), CancellationToken.None);
        //Assert
        Assert.AreEqual(4, result.products.TotalCount);
        Assert.AreEqual(4, result.products.Count);
    }

    [TestMethod]
    public async Task Handle_ListingPatternCleared_CountsAgain()
    {
        //Arrange
        await InsertVisibleProducts(3);
        await handler.Handle(Listing(pageSize: 2), CancellationToken.None);
        await InsertVisibleProducts(1);
        await _cache.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
        //Act
        var result = await handler.Handle(Listing(pageSize: 2), CancellationToken.None);
        //Assert
        Assert.AreEqual(4, result.products.TotalCount);
    }

    [TestMethod]
    public async Task Handle_ShowHidden_CountsLive()
    {
        //Arrange
        await InsertVisibleProducts(3);
        await handler.Handle(new GetSearchProductsQuery { ShowHidden = true, PageSize = 2 }, CancellationToken.None);
        await InsertVisibleProducts(1);
        //Act
        var result = await handler.Handle(new GetSearchProductsQuery { ShowHidden = true, PageSize = 2 },
            CancellationToken.None);
        //Assert
        Assert.AreEqual(4, result.products.TotalCount);
    }

    [TestMethod]
    public async Task Handle_DifferentCustomerGroups_DoNotShareTheCachedTotal()
    {
        //Arrange
        await InsertVisibleProducts(2);
        await _repository.InsertAsync(new Product {
            Published = true, VisibleIndividually = true, LimitedToGroups = true, CustomerGroups = ["members"]
        });
        //Act
        var members = await handler.Handle(
            new GetSearchProductsQuery { CustomerGroupIds = ["members"], PageSize = 1 }, CancellationToken.None);
        var guests = await handler.Handle(
            new GetSearchProductsQuery { CustomerGroupIds = ["guests"], PageSize = 1 }, CancellationToken.None);
        //Assert
        Assert.AreEqual(3, members.products.TotalCount);
        Assert.AreEqual(2, guests.products.TotalCount);
    }

    [TestMethod]
    public async Task Handle_DifferentCategories_DoNotShareTheCachedTotal()
    {
        //Arrange
        await InsertVisibleProducts(2, "shoes");
        await InsertVisibleProducts(3, "bags");
        //Act
        var shoes = await handler.Handle(Listing(pageSize: 1, categoryId: "shoes"), CancellationToken.None);
        var bags = await handler.Handle(Listing(pageSize: 1, categoryId: "bags"), CancellationToken.None);
        //Assert
        Assert.AreEqual(2, shoes.products.TotalCount);
        Assert.AreEqual(3, bags.products.TotalCount);
    }

    private static GetSearchProductsQuery Listing(int pageSize, string categoryId = null)
    {
        return new GetSearchProductsQuery {
            Customer = new Customer(), PageSize = pageSize,
            CategoryIds = categoryId == null ? null : [categoryId]
        };
    }

    private async Task InsertVisibleProducts(int count, string categoryId = null)
    {
        for (var i = 0; i < count; i++)
        {
            var product = new Product { Published = true, VisibleIndividually = true };
            if (categoryId != null)
                product.ProductCategories.Add(new ProductCategory { CategoryId = categoryId });
            await _repository.InsertAsync(product);
        }
    }

    private static GetSearchProductsQueryHandler FilteringHandler(IRepository<Product> repository,
        DatabaseConfig databaseConfig, IDatabaseContext databaseContext)
    {
        return new GetSearchProductsQueryHandler(repository, new Mock<ISpecificationAttributeService>().Object, Cache(),
            new CatalogSettings { IgnoreFilterableSpecAttributeOption = false }, new AccessControlConfig(),
            databaseConfig, databaseContext, NullLogger<GetSearchProductsQueryHandler>.Instance);
    }

    private static ICacheBase Cache()
    {
        return new MemoryCacheBase(MemoryCacheTest.Get(), Mock.Of<IMediator>(),
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
    }
}
