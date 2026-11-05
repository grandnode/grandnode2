using Grand.Business.Cms.Events.Cache;
using Grand.Domain.Common;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Pages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Events.Cache;

[TestClass]
public class CmsCacheEventHandlersTests
{
    [TestMethod]
    public async Task Knowledgebase_InsertedKnowledgebaseCategory_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<KnowledgebaseCategory>(new KnowledgebaseCategory());
        await new KnowledgebaseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Knowledgebase_UpdatedKnowledgebaseCategory_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<KnowledgebaseCategory>(new KnowledgebaseCategory());
        await new KnowledgebaseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Knowledgebase_DeletedKnowledgebaseCategory_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<KnowledgebaseCategory>(new KnowledgebaseCategory());
        await new KnowledgebaseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Knowledgebase_InsertedKnowledgebaseArticle_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<KnowledgebaseArticle>(new KnowledgebaseArticle());
        await new KnowledgebaseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Knowledgebase_UpdatedKnowledgebaseArticle_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<KnowledgebaseArticle>(new KnowledgebaseArticle());
        await new KnowledgebaseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Knowledgebase_DeletedKnowledgebaseArticle_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<KnowledgebaseArticle>(new KnowledgebaseArticle());
        await new KnowledgebaseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PageLayout_InsertedPageLayout_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<PageLayout>(new PageLayout());
        await new PageLayoutCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PageLayout_UpdatedPageLayout_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<PageLayout>(new PageLayout());
        await new PageLayoutCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PageLayout_DeletedPageLayout_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<PageLayout>(new PageLayout());
        await new PageLayoutCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Page_InsertedPage_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<Page>(new Page());
        await new PageCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Page_UpdatedPage_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<Page>(new Page());
        await new PageCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Page_DeletedPage_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<Page>(new Page());
        await new PageCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task RobotsTxt_InsertedRobotsTxt_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<RobotsTxt>(new RobotsTxt());
        await new RobotsTxtCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task RobotsTxt_UpdatedRobotsTxt_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<RobotsTxt>(new RobotsTxt());
        await new RobotsTxtCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task RobotsTxt_DeletedRobotsTxt_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<RobotsTxt>(new RobotsTxt());
        await new RobotsTxtCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
