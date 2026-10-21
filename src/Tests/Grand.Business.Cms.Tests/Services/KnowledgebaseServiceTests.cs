using Grand.Business.Cms.Services;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
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

    [TestInitialize]
    public void Init()
    {
        _repositoryKnowledgebaseCategory = new MongoDBRepositoryTest<KnowledgebaseCategory>();
        _repositoryKnowledgebaseArticle = new MongoDBRepositoryTest<KnowledgebaseArticle>();
        _repositoryKnowledgebaseArticleComment = new MongoDBRepositoryTest<KnowledgebaseArticleComment>();

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _knowledgebaseService = new KnowledgebaseService(_repositoryKnowledgebaseCategory,
            _repositoryKnowledgebaseArticle, _repositoryKnowledgebaseArticleComment, _mediatorMock.Object,
            _cacheBase, new AccessControlConfig());
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
        Assert.AreEqual(
            "test",
            _repositoryKnowledgebaseCategory.Table.FirstOrDefault(x => x.Id == knowledgebaseCategory.Id).Name);
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
        var result = await _knowledgebaseService.GetPublicKnowledgebaseCategory(knowledgebaseCategory.Id,
            CatalogAccess.For(new Customer(), ""));
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
    public async Task GetKnowledgebaseArticles_ReturnsGroupAndStoreLimitedArticles()
    {
        //Arrange
        var limited = new KnowledgebaseArticle {
            Published = true, LimitedToGroups = true, CustomerGroups = ["g1"], LimitedToStores = true, Stores = ["s1"]
        };
        await _repositoryKnowledgebaseArticle.InsertAsync(limited);
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticles();
        //Assert
        Assert.IsTrue(result.Any(x => x.Id == limited.Id));
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
        Assert.AreEqual("test", _repositoryKnowledgebaseArticle.Table.FirstOrDefault(x => x.Id == knowledgebaseArticle.Id).Name);
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
        var result = await _knowledgebaseService.GetPublicKnowledgebaseCategories(
            CatalogAccess.For(new Customer(), ""));
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
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticles(CatalogAccess.For(new Customer(), ""));
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
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticle(knowledgebaseArticle.Id,
            CatalogAccess.For(new Customer(), ""));
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
            await _knowledgebaseService.GetPublicKnowledgebaseArticlesByCategory(knowledgebaseArticle.ParentCategoryId,
                CatalogAccess.For(new Customer(), ""));
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
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticlesByKeyword("test",
            CatalogAccess.For(new Customer(), ""));
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
        var result = await _knowledgebaseService.GetPublicKnowledgebaseCategoriesByKeyword("test",
            CatalogAccess.For(new Customer(), ""));
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
        var result = await _knowledgebaseService.GetHomepageKnowledgebaseArticles(
            CatalogAccess.For(new Customer(), ""));
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
    public async Task GetRelatedKnowledgebaseArticles_KeepsRelationOrderAndSkipsDeleted()
    {
        //Arrange
        var first = new KnowledgebaseArticle { Name = "first" };
        var second = new KnowledgebaseArticle { Name = "second" };
        await _knowledgebaseService.InsertKnowledgebaseArticle(first);
        await _knowledgebaseService.InsertKnowledgebaseArticle(second);
        var article = new KnowledgebaseArticle();
        article.RelatedArticles.Add(second.Id);
        article.RelatedArticles.Add("deleted");
        article.RelatedArticles.Add(first.Id);
        await _knowledgebaseService.InsertKnowledgebaseArticle(article);
        //Act
        var result = await _knowledgebaseService.GetRelatedKnowledgebaseArticles(article.Id, 0, 1);
        //Assert
        Assert.AreEqual("second", result.Single().Name);
        Assert.AreEqual(2, result.TotalCount);
    }

    [TestMethod]
    public async Task GetRelatedKnowledgebaseArticles_KeepsDuplicateRelations()
    {
        //Arrange
        var related = new KnowledgebaseArticle { Name = "related" };
        await _knowledgebaseService.InsertKnowledgebaseArticle(related);
        var article = new KnowledgebaseArticle();
        article.RelatedArticles.Add(related.Id);
        article.RelatedArticles.Add(related.Id);
        await _knowledgebaseService.InsertKnowledgebaseArticle(article);
        //Act
        var result = await _knowledgebaseService.GetRelatedKnowledgebaseArticles(article.Id);
        //Assert
        Assert.AreEqual(2, result.TotalCount);
    }

    [TestMethod]
    public async Task GetRelatedKnowledgebaseArticles_UnknownArticle_ReturnsEmpty()
    {
        //Act
        var result = await _knowledgebaseService.GetRelatedKnowledgebaseArticles("unknown");
        //Assert
        Assert.AreEqual(0, result.TotalCount);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticlesByCategoryId_ReturnsRequestedPageAndTotal()
    {
        //Arrange
        for (var i = 0; i < 3; i++)
            await _knowledgebaseService.InsertKnowledgebaseArticle(new KnowledgebaseArticle {
                Name = $"article {i}", ParentCategoryId = "1", DisplayOrder = i
            });
        await _knowledgebaseService.InsertKnowledgebaseArticle(new KnowledgebaseArticle { ParentCategoryId = "2" });
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticlesByCategoryId("1", 1, 2);
        //Assert
        Assert.AreEqual("article 2", result.Single().Name);
        Assert.AreEqual(3, result.TotalCount);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticlesByName_ReturnsRequestedPageAndTotal()
    {
        //Arrange
        for (var i = 0; i < 3; i++)
            await _knowledgebaseService.InsertKnowledgebaseArticle(new KnowledgebaseArticle {
                Name = $"Shipping {i}", Published = true, DisplayOrder = i
            });
        await _knowledgebaseService.InsertKnowledgebaseArticle(new KnowledgebaseArticle {
            Name = "Returns", Published = true
        });
        //Act
        var result = await _knowledgebaseService.GetKnowledgebaseArticlesByName("shipping", 0, 2);
        //Assert
        CollectionAssert.AreEqual(new[] { "Shipping 0", "Shipping 1" }, result.Select(x => x.Name).ToArray());
        Assert.AreEqual(3, result.TotalCount);
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

    private static Customer CustomerIn(params string[] groupIds)
    {
        var customer = new Customer();
        foreach (var groupId in groupIds)
            customer.Groups.Add(groupId);
        return customer;
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseCategories_DifferentGroups_DoNotShareTheCachedList()
    {
        //Arrange
        var category = new KnowledgebaseCategory { Published = true, LimitedToGroups = true, CustomerGroups = ["g1"] };
        await _repositoryKnowledgebaseCategory.InsertAsync(category);
        //Act
        var forMember = await _knowledgebaseService.GetPublicKnowledgebaseCategories(
            CatalogAccess.For(CustomerIn("g1"), "s1"));
        var forOther = await _knowledgebaseService.GetPublicKnowledgebaseCategories(
            CatalogAccess.For(CustomerIn("g2"), "s1"));
        //Assert
        Assert.IsTrue(forMember.Any(x => x.Id == category.Id));
        Assert.IsFalse(forOther.Any(x => x.Id == category.Id));
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticles_IncludingHidden_DoesNotNeedACustomer()
    {
        //Arrange
        var article = new KnowledgebaseArticle { Published = true, LimitedToGroups = true, CustomerGroups = ["g1"] };
        await _repositoryKnowledgebaseArticle.InsertAsync(article);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticles(CatalogAccess.IncludingHidden());
        //Assert
        Assert.IsTrue(result.Any(x => x.Id == article.Id));
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticles_IncludingHiddenWithoutStore_ReturnsEveryStoresArticles()
    {
        //Arrange
        var article = new KnowledgebaseArticle { Published = true, LimitedToStores = true, Stores = ["s1"] };
        await _repositoryKnowledgebaseArticle.InsertAsync(article);
        //Act
        var result = await _knowledgebaseService.GetPublicKnowledgebaseArticles(CatalogAccess.IncludingHidden());
        //Assert
        Assert.IsTrue(result.Any(x => x.Id == article.Id));
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticles_IncludingHiddenThenNoGroups_DoNotShareTheCachedList()
    {
        //Arrange
        var article = new KnowledgebaseArticle { Published = true, LimitedToGroups = true, CustomerGroups = ["g1"] };
        await _repositoryKnowledgebaseArticle.InsertAsync(article);
        //Act
        var hidden = await _knowledgebaseService.GetPublicKnowledgebaseArticles(CatalogAccess.IncludingHidden("s1"));
        var noGroups = await _knowledgebaseService.GetPublicKnowledgebaseArticles(
            CatalogAccess.For(CustomerIn(), "s1"));
        //Assert
        Assert.IsTrue(hidden.Any(x => x.Id == article.Id));
        Assert.IsFalse(noGroups.Any(x => x.Id == article.Id));
    }

    [TestMethod]
    public async Task GetPublicKnowledgebaseArticles_WhenScopeGivenWithoutContext_FiltersByStoreAndGroups()
    {
        //Arrange - no ambient work or store context, as in a scheduled task
        var service = new KnowledgebaseService(_repositoryKnowledgebaseCategory,
            _repositoryKnowledgebaseArticle, _repositoryKnowledgebaseArticleComment, _mediatorMock.Object,
            _cacheBase, new AccessControlConfig());
        await _repositoryKnowledgebaseArticle.InsertAsync(new KnowledgebaseArticle { Id = "open", Published = true });
        await _repositoryKnowledgebaseArticle.InsertAsync(new KnowledgebaseArticle {
            Id = "guests", Published = true, LimitedToGroups = true, CustomerGroups = ["guests-group"]
        });
        await _repositoryKnowledgebaseArticle.InsertAsync(new KnowledgebaseArticle {
            Id = "admins", Published = true, LimitedToGroups = true, CustomerGroups = ["admins-group"]
        });
        await _repositoryKnowledgebaseArticle.InsertAsync(new KnowledgebaseArticle {
            Id = "other-store", Published = true, LimitedToStores = true, Stores = ["store2"]
        });
        await _repositoryKnowledgebaseArticle.InsertAsync(new KnowledgebaseArticle { Id = "unpublished" });
        //Act
        var result = await service.GetPublicKnowledgebaseArticles("store1", ["guests-group"]);
        //Assert
        CollectionAssert.AreEquivalent(new[] { "open", "guests" }, result.Select(x => x.Id).ToList());
    }
}