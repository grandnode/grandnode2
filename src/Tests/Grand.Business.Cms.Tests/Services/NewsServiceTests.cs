using Grand.Business.Cms.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.News;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Services;

[TestClass]
public class NewsServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private NewsService _newsService;

    private IRepository<NewsItem> _repository;
    private Mock<IContextAccessor> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<NewsItem>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IContextAccessor>();

        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());

        _newsService = new NewsService(_repository, _mediatorMock.Object, _workContextMock.Object,
            new AccessControlConfig());
    }

    [TestMethod]
    public async Task GetNewsByIdTest()
    {
        //Arrange
        var newsItem = new NewsItem();
        await _repository.InsertAsync(newsItem);
        //Act
        var result = await _newsService.GetNewsById(newsItem.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetAllNewsTest()
    {
        //Arrange
        var newsItem = new NewsItem { Published = true };
        await _repository.InsertAsync(newsItem);
        //Act
        var result = await _newsService.GetAllNews();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task InsertNewsTest()
    {
        //Arrange
        var newsItem = new NewsItem();
        //Act
        await _newsService.InsertNews(newsItem);
        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == newsItem.Id));
    }

    [TestMethod]
    public async Task UpdateNewsTest()
    {
        //Arrange
        var newsItem = new NewsItem { Published = true };
        await _repository.InsertAsync(newsItem);
        //Act
        newsItem.Title = "test";
        await _newsService.UpdateNews(newsItem);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == newsItem.Id).Title == "test");
    }

    [TestMethod]
    public async Task DeleteNewsTest()
    {
        //Arrange
        var newsItem = new NewsItem { Published = true };
        await _repository.InsertAsync(newsItem);
        //Act
        await _newsService.DeleteNews(newsItem);
        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == newsItem.Id));
    }

    [TestMethod]
    public async Task GetAllCommentsTest()
    {
        //Arrange
        var newsItem = new NewsItem { Published = true };
        newsItem.NewsComments.Add(new NewsComment { CustomerId = "1" });
        await _repository.InsertAsync(newsItem);
        //Act
        var result = await _newsService.GetAllComments("1");
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetStoreNews_NoLimit_ReturnsAllPublishedNews()
    {
        //Arrange
        await _repository.InsertAsync(new NewsItem { Published = true, Title = "News 1" });
        await _repository.InsertAsync(new NewsItem { Published = true, Title = "News 2" });
        await _repository.InsertAsync(new NewsItem { Published = false, Title = "News 3" }); // Should be excluded
        //Act
        var result = await _newsService.GetStoreNews("", 0, 10, 0); // No limit (storeNewsLimit = 0)
        //Assert
        Assert.AreEqual(2, result.Count); // Only published news
        Assert.IsTrue(result.All(x => x.Published));
    }

    [TestMethod]
    public async Task GetStoreNews_WithLimit_ReturnsLimitedNews()
    {
        //Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _repository.InsertAsync(new NewsItem { Published = true, Title = $"News {i}" });
        }
        //Act
        var result = await _newsService.GetStoreNews("", 0, 10, 5); // Limit to 5 items
        //Assert
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual(5, result.TotalCount); // Total should respect the limit
    }

    [TestMethod]
    public async Task GetStoreNews_WithLimitAndPagination_ReturnsCorrectPage()
    {
        //Arrange
        for (int i = 1; i <= 15; i++)
        {
            await _repository.InsertAsync(new NewsItem { Published = true, Title = $"News {i}" });
        }
        //Act - Request page 2 (index 1) with page size 5, but limit to 8 total items
        var result = await _newsService.GetStoreNews("", 1, 5, 8); // Page 2, size 5, limit 8
        //Assert
        Assert.AreEqual(3, result.Count); // Should have 3 items (8 total - 5 on first page = 3 remaining)
    }

    [TestMethod]
    public async Task GetStoreNews_PageExceedsLimit_ReturnsEmptyResult()
    {
        //Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _repository.InsertAsync(new NewsItem { Published = true, Title = $"News {i}" });
        }
        //Act - Request page 3 (index 2) with page size 5, but limit to 8 total items
        var result = await _newsService.GetStoreNews("", 2, 5, 8); // Page 3, size 5, limit 8 (no items left)
        //Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetStoreNews_ExcludesUnpublishedNews()
    {
        //Arrange
        await _repository.InsertAsync(new NewsItem { Published = true, Title = "Published News" });
        await _repository.InsertAsync(new NewsItem { Published = false, Title = "Unpublished News" });
        //Act
        var result = await _newsService.GetStoreNews("", 0, 10, 0);
        //Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Published News", result.First().Title);
    }
}