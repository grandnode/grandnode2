using Grand.Business.Storage.Events.Cache;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Storage.Tests.Events.Cache;

[TestClass]
public class StorageCacheEventHandlersTests
{
    [TestMethod]
    public async Task Picture_UpdatedPicture_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<Picture>(new Picture { Id = "pic1" });
        await new PictureCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, notification.Entity.Id), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
