using Grand.Business.Cms.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Services;

[TestClass]
public class KnowledgebaseServiceTests
{
    private MemoryCacheBase _cacheBase;

    private KnowledgebaseService _knowledgebaseService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<KnowledgebaseArticle> _repositoryKnowledgebaseArticle;
    private IRepository<KnowledgebaseArticleComment> _repositoryKnowledgebaseArticleComment;

    private IRepository<KnowledgebaseCategory> _repositoryKnowledgebaseCategory;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repositoryKnowledgebaseCategory = new MongoDBRepositoryTest<KnowledgebaseCategory>();
        _repositoryKnowledgebaseArticle = new MongoDBRepositoryTest<KnowledgebaseArticle>();
        _repositoryKnowledgebaseArticleComment = new MongoDBRepositoryTest<KnowledgebaseArticleComment>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();

        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _knowledgebaseService = new KnowledgebaseService(_repositoryKnowledgebaseCategory,
            _repositoryKnowledgebaseArticle, _repositoryKnowledgebaseArticleComment, _mediatorMock.Object,
            _workContextMock.Object, _cacheBase, new AccessControlConfig());
    }

    [TestMethod]
    public async Task DeleteKnowledgebaseCategoryTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory();
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        await _knowledgebaseService.DeleteKnowledgebaseCategory(knowledgebaseCategory);
        //Assert
        Assert.IsFalse(_repositoryKnowledgebaseCategory.Table.Any());
    }

    [TestMethod]
    public async Task UpdateKnowledgebaseCategoryTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory();
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        knowledgebaseCategory.Name = "test";
        await _knowledgebaseService.UpdateKnowledgebaseCategory(knowledgebaseCategory);
        //Assert
        Assert.IsTrue(
            _repositoryKnowledgebaseCategory.Table.FirstOrDefault(x => x.Id == knowledgebaseCategory.Id).Name ==
            "test");
    }

    [TestMethod]
    public async Task GetKnowledgebaseCategoryTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory();
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseCategory(knowledgebaseCategory.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseCategoryTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory { Published = true };
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseCategory(knowledgebaseCategory.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertKnowledgebaseCategoryTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory();
        //Act
        await _knowledgebaseService.InsertKnowledgebaseCategory(knowledgebaseCategory);
        var result = await _knowledgebaseService.GetKnowledgebaseCategory(knowledgebaseCategory.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetKnowledgebaseCategoriesTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory { Published = true };
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseCategories();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticleTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        await _repositoryKnowledgebaseArticle.InsertAsync(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticle(knowledgebaseArticle.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticlesTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        await _repositoryKnowledgebaseArticle.InsertAsync(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticles();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task InsertKnowledgebaseArticleTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        //Act
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Assert
        Assert.IsTrue(_repositoryKnowledgebaseArticle.Table.Any());
    }

    [TestMethod]
    public async Task UpdateKnowledgebaseArticleTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        knowledgebaseArticle.Name = "test";
        await _knowledgebaseService.UpdateKnowledgebaseArticle(knowledgebaseArticle);
        //Assert
        Assert.IsTrue(_repositoryKnowledgebaseArticle.Table.FirstOrDefault(x => x.Id == knowledgebaseArticle.Id).Name ==
                      "test");
    }

    [TestMethod]
    public async Task DeleteKnowledgebaseArticleTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        await _knowledgebaseService.DeleteKnowledgebaseArticle(knowledgebaseArticle);
        //Assert
        Assert.IsNull(_repositoryKnowledgebaseArticle.Table.FirstOrDefault(x => x.Id == knowledgebaseArticle.Id));
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticlesByCategoryIdTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true, ParentCategoryId = "1" };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticlesByCategoryId("1");
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseCategoriesTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory { Published = true };
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseCategories();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticlesTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticles();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticleTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticle(knowledgebaseArticle.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticlesByCategoryTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true, ParentCategoryId = "1" };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result =
            await _knowledgebaseService.GetPublicKnowledgebaseArticlesByCategory(knowledgebaseArticle.ParentCategoryId);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticlesByKeywordTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true, Name = "test" };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticlesByKeyword("test");
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseCategoriesByKeywordTest()
    {
        //Arrange
        var knowledgebaseCategory = new KnowledgebaseCategory { Published = true, Name = "test" };
        await _repositoryKnowledgebaseCategory.InsertAsync(knowledgebaseCategory);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseCategoriesByKeyword("test");
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetHomepageKnowledgebaseArticlesTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true, Name = "test", ShowOnHomepage = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetHomepageKnowledgebaseArticles();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticlesByNameTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true, Name = "test", ShowOnHomepage = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticlesByName(knowledgebaseArticle.Name);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetRelatedKnowledgebaseArticlesTest()
    {
        //Arrange
        var knowledgebaseArticle = new KnowledgebaseArticle { Published = true, Name = "test", ShowOnHomepage = true };
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle);
        var knowledgebaseArticle1 = new KnowledgebaseArticle();
        knowledgebaseArticle1.RelatedArticles.Add(knowledgebaseArticle.Id);
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgebaseArticle1);
        //Act
        var result = await _knowledgebaseService.GetRelatedKnowledgebaseArticles(knowledgebaseArticle1.Id);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task InsertArticleCommentTest()
    {
        //Arrange
        var knowledgebaseArticleComment = new KnowledgebaseArticleComment();
        //Act
        await _knowledgebaseService.InsertArticleComment(knowledgebaseArticleComment);
        //Assert
        Assert.IsTrue(_repositoryKnowledgebaseArticleComment.Table.Any());
    }

    [TestMethod]
    public async Task GetAllCommentsTest()
    {
        //Arrange
        var knowledgebaseArticleComment = new KnowledgebaseArticleComment { CustomerId = "1" };
        await _knowledgebaseService.InsertArticleComment(knowledgebaseArticleComment);
        //Act
        var result = await _knowledgebaseService.GetAllComments("1");
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetArticleCommentByIdTest()
    {
        //Arrange
        var knowledgebaseArticleComment = new KnowledgebaseArticleComment { CustomerId = "1" };
        await _knowledgebaseService.InsertArticleComment(knowledgebaseArticleComment);
        //Act
        var result = await _knowledgebaseService.GetArticleCommentById(knowledgebaseArticleComment.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetArticleCommentsByIdsTest()
    {
        //Arrange
        var knowledgebaseArticleComment = new KnowledgebaseArticleComment { CustomerId = "1" };
        await _knowledgebaseService.InsertArticleComment(knowledgebaseArticleComment);
        //Act
        var result = await _knowledgebaseService.GetArticleCommentsByIds([knowledgebaseArticleComment.Id]);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetArticleCommentsByArticleIdTest()
    {
        //Arrange
        var knowledgebaseArticleComment = new KnowledgebaseArticleComment { CustomerId = "1", ArticleId = "1" };
        await _knowledgebaseService.InsertArticleComment(knowledgebaseArticleComment);
        //Act
        var result = await _knowledgebaseService.GetArticleCommentsByArticleId(knowledgebaseArticleComment.ArticleId);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task DeleteArticleCommentTest()
    {
        //Arrange
        var knowledgebaseArticleComment = new KnowledgebaseArticleComment { CustomerId = "1" };
        await _knowledgebaseService.InsertArticleComment(knowledgebaseArticleComment);
        //Act
        await _knowledgebaseService.DeleteArticleComment(knowledgebaseArticleComment);
        //Assert
        Assert.IsNull(
            _repositoryKnowledgebaseArticleComment.Table.FirstOrDefault(x => x.Id == knowledgebaseArticleComment.Id));
    }
}