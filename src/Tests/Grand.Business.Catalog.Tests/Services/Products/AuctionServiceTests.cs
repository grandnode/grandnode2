using Grand.Business.Catalog.Services.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class AuctionServiceTests
{
    private AuctionService _auctionService;
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _productrepository;
    private IRepository<Bid> _repository;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _repository = new MongoDBRepositoryTest<Bid>();
        _productrepository = new MongoDBRepositoryTest<Product>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _auctionService = new AuctionService(_repository, _productrepository, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetBidTest()
    {
        //Arrange
        var bid = new Bid {
            Amount = 10
        };
        await _auctionService.InsertBid(bid);

        //Act
        var result = await _auctionService.GetBid(bid.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.Amount);
    }

    [TestMethod]
    public async Task GetLatestBidTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        var bid2 = new Bid {
            Amount = 12,
            ProductId = "1",
            Date = DateTime.UtcNow
        };
        await _auctionService.InsertBid(bid2);

        //Act
        var result = await _auctionService.GetLatestBid("1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12, result.Amount);
    }

    [TestMethod]
    public async Task GetBidsByProductIdTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        var bid2 = new Bid {
            Amount = 12,
            ProductId = "1",
            Date = DateTime.UtcNow
        };
        await _auctionService.InsertBid(bid2);

        //Act
        var result = await _auctionService.GetBidsByProductId("1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetBidsByCustomerIdTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            CustomerId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        var bid2 = new Bid {
            Amount = 12,
            ProductId = "1",
            CustomerId = "2",
            Date = DateTime.UtcNow
        };
        await _auctionService.InsertBid(bid2);

        //Act
        var result = await _auctionService.GetBidsByCustomerId("1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task InsertBidTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            CustomerId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(1, _repository.Table.Count());
    }

    [TestMethod]
    public async Task UpdateBidTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            CustomerId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        bid1.Amount = 12;
        //Act
        await _auctionService.UpdateBid(bid1);
        //Assert
        Assert.AreEqual(12, _repository.Table.FirstOrDefault().Amount);
    }

    [TestMethod]
    public async Task DeleteBidTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            CustomerId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        //Act
        await _auctionService.DeleteBid(bid1);
        //Assert
        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public async Task UpdateHighestBidTest()
    {
        //Arrange
        var product = new Product();
        _productrepository.Insert(product);

        //Act
        await _auctionService.UpdateHighestBid(product, 15, "sample");

        //Assert
        Assert.AreEqual(15, _productrepository.Table.FirstOrDefault(x => x.Id == product.Id).HighestBid);
    }

    [TestMethod]
    public async Task GetAuctionsToEndTest()
    {
        //Arrange
        var product1 = new Product {
            ProductTypeId = ProductType.Auction,
            AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1)
        };
        _productrepository.Insert(product1);
        var product2 = new Product {
            ProductTypeId = ProductType.Auction,
            AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(-1)
        };
        _productrepository.Insert(product2);

        //Act
        var result = await _auctionService.GetAuctionsToEnd();

        //Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task UpdateAuctionEndedTest()
    {
        //Arrange
        var product1 = new Product {
            ProductTypeId = ProductType.Auction,
            AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1)
        };
        _productrepository.Insert(product1);

        //Act
        await _auctionService.UpdateAuctionEnded(product1, true, true);

        //Assert
        Assert.AreEqual(true, _productrepository.Table.FirstOrDefault(x => x.Id == product1.Id).AuctionEnded);
    }

    [TestMethod]
    public async Task NewBidTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            CustomerId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        //Act
        await _auctionService.NewBid(new Customer { Id = "1" }, new Product { Id = "1" }, new Store(), new Language(),
            "", 12);
        //Assert
        Assert.AreEqual(12, _repository.Table.Where(x => x.ProductId == "1").Max(x => x.Amount));
    }

    [TestMethod]
    public async Task CancelBidByOrderTest()
    {
        //Arrange
        var bid1 = new Bid {
            Amount = 10,
            ProductId = "1",
            CustomerId = "1",
            OrderId = "1",
            Date = DateTime.UtcNow.AddDays(-1)
        };
        await _auctionService.InsertBid(bid1);
        //Act
        await _auctionService.CancelBidByOrder("1");
        //Assert
        Assert.AreEqual(0, _repository.Table.Count());
    }
}