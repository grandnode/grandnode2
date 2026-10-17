using Grand.Business.Cms.Services;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.News;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Services;

[TestClass]
public class NewsServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private NewsService _newsService;

    private IRepository<NewsItem> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<NewsItem>();

        _mediatorMock = new Mock<IMediator>();
        _newsService = new NewsService(_repository, _mediatorMock.Object, new AccessControlConfig());
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
        var result = await _newsService.GetAllNews(CatalogAccess.For(new Customer(), ""));
        //Assert
        Assert.IsTrue(result.Any());
    }

    private static Customer CustomerIn(params string[] groupIds)
    {
        var customer = new Customer();
        foreach (var groupId in groupIds)
            customer.Groups.Add(groupId);
        return customer;
    }

    [TestMethod]
    public async Task GetAllNews_For_FiltersByTheGivenCustomersGroups()
    {
        //Arrange
        var newsItem = new NewsItem { Published = true, LimitedToGroups = true, CustomerGroups = ["g1"] };
        await _repository.InsertAsync(newsItem);
        //Act
        var forMember = await _newsService.GetAllNews(CatalogAccess.For(CustomerIn("g1"), ""));
        var forOther = await _newsService.GetAllNews(CatalogAccess.For(CustomerIn("g2"), ""));
        var includingHidden = await _newsService.GetAllNews(CatalogAccess.IncludingHidden());
        //Assert
        Assert.IsTrue(forMember.Any(x => x.Id == newsItem.Id));
        Assert.IsFalse(forOther.Any(x => x.Id == newsItem.Id));
        Assert.IsTrue(includingHidden.Any(x => x.Id == newsItem.Id));
    }

    [TestMethod]
    public async Task GetAllNews_EmptyStore_DoesNotFilterByStore()
    {
        //Arrange
        var newsItem = new NewsItem { Published = true, LimitedToStores = true, Stores = ["s1"] };
        await _repository.InsertAsync(newsItem);
        //Act
        var result = await _newsService.GetAllNews(CatalogAccess.For(new Customer(), ""));
        //Assert
        Assert.IsTrue(result.Any(x => x.Id == newsItem.Id));
    }

    [TestMethod]
    public async Task GetAllNews_IncludeUnpublished_ReturnsUnpublishedItems()
    {
        //Arrange
        var newsItem = new NewsItem { Published = false };
        await _repository.InsertAsync(newsItem);
        //Act
        var published = await _newsService.GetAllNews(CatalogAccess.IncludingHidden());
        var all = await _newsService.GetAllNews(CatalogAccess.IncludingHidden(), includeUnpublished: true);
        //Assert
        Assert.IsFalse(published.Any(x => x.Id == newsItem.Id));
        Assert.IsTrue(all.Any(x => x.Id == newsItem.Id));
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
        Assert.AreEqual("test", _repository.Table.FirstOrDefault(x => x.Id == newsItem.Id).Title);
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
}