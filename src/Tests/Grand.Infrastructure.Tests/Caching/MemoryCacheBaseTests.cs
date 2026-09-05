using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Tests.Caching;

[TestClass]
public class MemoryCacheBaseTests
{
    private CacheConfig _config;
    private Mock<IMediator> _mediatorMock;
    private IMemoryCache _memoryCache;
    private MemoryCacheBase _service;

    [TestInitialize]
    public void Init()
    {
        _config = new CacheConfig { DefaultCacheTimeMinutes = 1 };
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton(_config);
        var serviceProvider = services.BuildServiceProvider();

        _memoryCache = serviceProvider.GetService<IMemoryCache>();
        _mediatorMock = new Mock<IMediator>();
        _service = new MemoryCacheBase(_memoryCache, _mediatorMock.Object, _config);
    }
    [TestMethod]
    public void GetTest()
    {
        var result = _service.Get("key", () => { return "test"; });
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void GetTest_CacheTimeMinutes()
    {
        var result = _service.Get("key", () => { return "test"; }, 1);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public async Task GetAsyncTest()
    {
        var result = await _service.GetAsync("key", () => { return Task.FromResult("test"); });
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public async Task GetAsyncTest_CacheTimeMinutes()
    {
        var result = await _service.GetAsync("key", () => { return Task.FromResult("test"); }, 1);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public async Task RemoveAsyncTest_IsNull()
    {
        await _service.GetAsync("key", () => { return Task.FromResult("test"); }, 1);
        await _service.RemoveAsync("key");
        var result = _memoryCache.Get("key");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task RemoveAsyncTest_NotNull()
    {
        await _service.GetAsync("key", () => { return Task.FromResult("test"); }, 1);
        await _service.RemoveAsync("key1");
        var result = _memoryCache.Get("key");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task RemoveByPrefixTest()
    {
        await _service.GetAsync("key1", () => { return Task.FromResult("test"); }, 1);
        await _service.GetAsync("key2", () => { return Task.FromResult("test"); }, 1);
        await _service.GetAsync("test", () => { return Task.FromResult("test"); }, 1);
        await _service.RemoveByPrefix("key");
        var result = _memoryCache.Get("key1");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ClearTest()
    {
        await _service.GetAsync("key1", () => { return Task.FromResult("test"); }, 1);
        await _service.GetAsync("key2", () => { return Task.FromResult("test"); }, 1);
        await _service.GetAsync("test", () => { return Task.FromResult("test"); }, 1);
        await _service.Clear();
        var result = _memoryCache.Get("key1");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SetAsync_Key_NotExist_ShouldSetCacheEntry()
    {
        // Arrange
        var key = "testKey";
        var cacheTime = 60;
        var cacheEntry = "testValue";

        var acquireMock = new Mock<Func<Task<string>>>();
        acquireMock.Setup(a => a.Invoke()).ReturnsAsync(cacheEntry);

        // Act
        var result = await _service.SetAsync(key, acquireMock.Object, cacheTime);

        // Assert
        acquireMock.Verify(a => a.Invoke(), Times.Once);
        var cacheResult = _memoryCache.Get(key);
        Assert.IsNotNull(cacheResult);
        Assert.AreEqual(cacheEntry, cacheResult);
    }

    [TestMethod]
    public async Task SetAsync_Key_Exist_ShouldSetCacheEntry()
    {
        // Arrange
        var key = "testKey";
        var cacheTime = 60;
        var cacheEntry = "testValue";

        var acquireMock = new Mock<Func<Task<string>>>();
        acquireMock.Setup(a => a.Invoke()).ReturnsAsync(cacheEntry);
        await _service.GetAsync(key, () => { return Task.FromResult("fakeValue"); }, 1);

        // Act
        var result = await _service.SetAsync(key, acquireMock.Object, cacheTime);

        // Assert
        acquireMock.Verify(a => a.Invoke(), Times.Once);
        var cacheResult = _memoryCache.Get(key);
        Assert.IsNotNull(cacheResult);
        Assert.AreEqual(cacheEntry, cacheResult);
    }

    [TestMethod]
    public async Task RemoveAsync_AwaitsTheNotification()
    {
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<EntityCacheEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));

        //a fire-and-forget Publish would swallow this
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RemoveAsync("key"));
    }

    [TestMethod]
    public async Task RemoveByPrefix_AwaitsTheNotification()
    {
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<EntityCacheEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RemoveByPrefix("key"));
    }

    /// <summary>
    ///     Guards against disposing the reset token in <see cref="MemoryCacheBase.Clear" />.
    /// </summary>
    /// <remarks>
    ///     A writer reads the token source, then MemoryCache registers an eviction callback on it while
    ///     storing the entry. Disposing the previous source in Clear makes that registration throw
    ///     ObjectDisposedException, and swapping the field first only narrows the window rather than
    ///     closing it. The assertion only fires on an exception actually raised by the race, so this
    ///     cannot fail spuriously - it can only miss.
    /// </remarks>
    [TestMethod]
    [Timeout(60000)]
    [DoNotParallelize]
    public void Clear_WhileEntriesAreBeingWritten_DoesNotThrow()
    {
        Exception captured = null;
        var stopWriting = false;

        var writer = Task.Run(async () =>
        {
            var i = 0;
            while (!stopWriting)
                try
                {
                    await _service.SetAsync($"race-{i++}", () => Task.FromResult("value"));
                }
                catch (Exception ex)
                {
                    captured ??= ex;
                    return;
                }
        });

        for (var i = 0; i < 5000 && captured == null; i++) _service.Clear(false).GetAwaiter().GetResult();

        stopWriting = true;
        writer.Wait(TimeSpan.FromSeconds(5));

        Assert.IsNull(captured, $"Clear raced a concurrent write: {captured}");
    }

    [TestMethod]
    public async Task Dispose_OneInstance_DoesNotBreakAnotherInstance()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var firstCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var secondCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var first = new MemoryCacheBase(firstCache, new Mock<IMediator>().Object, _config);
        var second = new MemoryCacheBase(secondCache, new Mock<IMediator>().Object, _config);

        first.Dispose();

        var result = await second.GetAsync("key", () => Task.FromResult("value"));

        Assert.AreEqual("value", result);
    }
}