using Grand.Business.Cms.Events;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Events;

[TestClass]
public class ContentDeletedSlugEventHandlerTests
{
    private IRepository<EntityUrl> _entityUrlRepository;
    private ContentDeletedSlugEventHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
        var cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _handler = new ContentDeletedSlugEventHandler(_entityUrlRepository, cacheBase);
    }

    private async Task Seed(string id, string entityName)
    {
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = id, EntityName = entityName, Slug = id + "-en" });
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = id, EntityName = entityName, Slug = id + "-pl", LanguageId = "pl" });
        //another entity's url must survive
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = "other", EntityName = entityName, Slug = "other-" + entityName });
    }

    private void AssertOnlyOtherLeft(string id, string entityName)
    {
        Assert.AreEqual(0, _entityUrlRepository.Table.Count(x => x.EntityId == id && x.EntityName == entityName));
        Assert.AreEqual(1, _entityUrlRepository.Table.Count(x => x.EntityId == "other" && x.EntityName == entityName));
    }

    [TestMethod]
    public async Task Page_DeletesItsUrls()
    {
        var page = new Page(); await Seed(page.Id, EntityTypes.Page);
        await _handler.Handle(new EntityDeleted<Page>(page), CancellationToken.None);
        AssertOnlyOtherLeft(page.Id, EntityTypes.Page);
    }

    [TestMethod]
    public async Task NewsItem_DeletesItsUrls()
    {
        var item = new NewsItem(); await Seed(item.Id, EntityTypes.NewsItem);
        await _handler.Handle(new EntityDeleted<NewsItem>(item), CancellationToken.None);
        AssertOnlyOtherLeft(item.Id, EntityTypes.NewsItem);
    }

    [TestMethod]
    public async Task KnowledgebaseArticle_DeletesItsUrls()
    {
        var article = new KnowledgebaseArticle(); await Seed(article.Id, EntityTypes.KnowledgeBaseArticle);
        await _handler.Handle(new EntityDeleted<KnowledgebaseArticle>(article), CancellationToken.None);
        AssertOnlyOtherLeft(article.Id, EntityTypes.KnowledgeBaseArticle);
    }

    [TestMethod]
    public async Task KnowledgebaseCategory_DeletesItsUrls()
    {
        var category = new KnowledgebaseCategory(); await Seed(category.Id, EntityTypes.KnowledgeBaseCategory);
        await _handler.Handle(new EntityDeleted<KnowledgebaseCategory>(category), CancellationToken.None);
        AssertOnlyOtherLeft(category.Id, EntityTypes.KnowledgeBaseCategory);
    }
}
