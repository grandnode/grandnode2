using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Caching.Redis;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Tests.Caching.Redis;

[TestClass]
public class RedisMessageCacheManagerTests
{
    private Mock<IMessageBus> _messageBusMock;
    private IMemoryCache _memoryCache;
    private RedisMessageCacheManager _service;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        _memoryCache = services.BuildServiceProvider().GetService<IMemoryCache>();
        _messageBusMock = new Mock<IMessageBus>();
        _service = new RedisMessageCacheManager(_memoryCache, new Mock<IMediator>().Object,
            _messageBusMock.Object, new CacheConfig { DefaultCacheTimeMinutes = 1 });
    }

    [TestMethod]
    public async Task RemoveAsync_RemovesKeyAndPublishesMessage()
    {
        await _service.GetAsync("key", () => Task.FromResult("value"));

        await _service.RemoveAsync("key");

        Assert.IsFalse(_memoryCache.TryGetValue("key", out _));
        _messageBusMock.Verify(m => m.PublishAsync(It.Is<MessageEvent>(e =>
            e.Key == "key" && e.MessageType == (int)MessageEventType.RemoveKey)), Times.Once);
    }

    [TestMethod]
    public async Task RemoveAsync_PublisherDisabled_DoesNotPublish()
    {
        await _service.GetAsync("key", () => Task.FromResult("value"));

        await _service.RemoveAsync("key", false);

        Assert.IsFalse(_memoryCache.TryGetValue("key", out _));
        _messageBusMock.Verify(m => m.PublishAsync(It.IsAny<MessageEvent>()), Times.Never);
    }

    [TestMethod]
    public async Task RemoveByPrefix_RemovesMatchingKeysAndPublishesMessage()
    {
        await _service.GetAsync("prefix-1", () => Task.FromResult("value"));
        await _service.GetAsync("prefix-2", () => Task.FromResult("value"));
        await _service.GetAsync("other", () => Task.FromResult("value"));

        await _service.RemoveByPrefix("prefix");

        Assert.IsFalse(_memoryCache.TryGetValue("prefix-1", out _));
        Assert.IsFalse(_memoryCache.TryGetValue("prefix-2", out _));
        Assert.IsTrue(_memoryCache.TryGetValue("other", out _));
        _messageBusMock.Verify(m => m.PublishAsync(It.Is<MessageEvent>(e =>
            e.Key == "prefix" && e.MessageType == (int)MessageEventType.RemoveByPrefix)), Times.Once);
    }

    [TestMethod]
    public async Task RemoveByPrefix_PublisherDisabled_DoesNotPublish()
    {
        await _service.GetAsync("prefix-1", () => Task.FromResult("value"));

        await _service.RemoveByPrefix("prefix", false);

        Assert.IsFalse(_memoryCache.TryGetValue("prefix-1", out _));
        _messageBusMock.Verify(m => m.PublishAsync(It.IsAny<MessageEvent>()), Times.Never);
    }

    [TestMethod]
    public async Task Clear_PublishesClearCacheMessage()
    {
        await _service.GetAsync("key", () => Task.FromResult("value"));

        await _service.Clear();

        Assert.IsFalse(_memoryCache.TryGetValue("key", out _));
        _messageBusMock.Verify(m => m.PublishAsync(It.Is<MessageEvent>(e =>
            e.MessageType == (int)MessageEventType.ClearCache)), Times.Once);
    }

    [TestMethod]
    public async Task Clear_PublisherDisabled_DoesNotPublish()
    {
        await _service.GetAsync("key", () => Task.FromResult("value"));

        await _service.Clear(false);

        Assert.IsFalse(_memoryCache.TryGetValue("key", out _));
        _messageBusMock.Verify(m => m.PublishAsync(It.IsAny<MessageEvent>()), Times.Never);
    }
}
