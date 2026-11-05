using Grand.Business.Messages.Events.Cache;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Events.Cache;

[TestClass]
public class MessagesCacheEventHandlersTests
{
    [TestMethod]
    public async Task EmailAccount_InsertedEmailAccount_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<EmailAccount>(new EmailAccount());
        await new EmailAccountCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task EmailAccount_UpdatedEmailAccount_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<EmailAccount>(new EmailAccount());
        await new EmailAccountCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task EmailAccount_DeletedEmailAccount_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<EmailAccount>(new EmailAccount());
        await new EmailAccountCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MessageTemplate_InsertedMessageTemplate_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<MessageTemplate>(new MessageTemplate());
        await new MessageTemplateCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MessageTemplate_UpdatedMessageTemplate_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<MessageTemplate>(new MessageTemplate());
        await new MessageTemplateCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MessageTemplate_DeletedMessageTemplate_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<MessageTemplate>(new MessageTemplate());
        await new MessageTemplateCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
