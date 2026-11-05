using Grand.Domain.Admin;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Web.Common.Events.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Events.Cache;

[TestClass]
public class AdminSiteMapCacheEventHandlerTests
{
    [TestMethod]
    public async Task AdminSiteMap_InsertedAdminSiteMap_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<AdminSiteMap>(new AdminSiteMap());
        await new AdminSiteMapCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task AdminSiteMap_UpdatedAdminSiteMap_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<AdminSiteMap>(new AdminSiteMap());
        await new AdminSiteMapCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task AdminSiteMap_DeletedAdminSiteMap_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<AdminSiteMap>(new AdminSiteMap());
        await new AdminSiteMapCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
