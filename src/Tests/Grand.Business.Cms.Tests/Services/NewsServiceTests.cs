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
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<NewsItem>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();

        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

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
}