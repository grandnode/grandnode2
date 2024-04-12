using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using MediatR;
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
    public async Task GetAsyncTest()
    {
        var result = await _service.GetAsync("key", () => { return Task.FromResult("test"); });
        Assert.AreEqual(result, "test");
    }

    [TestMethod]
    public async Task GetAsyncTest_CacheTimeMinutes()
    {
        var result = await _service.GetAsync("key", () => { return Task.FromResult("test"); }, 1);
        Assert.AreEqual(result, "test");
    }

    [TestMethod]
    public void GetTest()
    {
        var result = _service.Get("key", () => { return "test"; });
        Assert.AreEqual(result, "test");
    }

    [TestMethod]
    public void GetTest_CacheTimeMinutes()
    {
        var result = _service.Get("key", () => { return "test"; }, 1);
        Assert.AreEqual(result, "test");
    }

    [TestMethod]
    public async Task RemoveAsyncTest_IsNull()
    {
        _service.Get("key", () => { return "test"; }, 1);
        await _service.RemoveAsync("key");
        var result = _memoryCache.Get("key");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task RemoveAsyncTest_NotNull()
    {
        _service.Get("key", () => { return "test"; }, 1);
        await _service.RemoveAsync("key1");
        var result = _memoryCache.Get("key");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task RemoveByPrefixTest()
    {
        _service.Get("key1", () => { return "test"; }, 1);
        _service.Get("key2", () => { return "test"; }, 1);
        _service.Get("test", () => { return "test"; }, 1);
        await _service.RemoveByPrefix("key");
        var result = _memoryCache.Get("key1");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ClearTest()
    {
        _service.Get("key1", () => { return "test"; }, 1);
        _service.Get("key2", () => { return "test"; }, 1);
        _service.Get("test", () => { return "test"; }, 1);
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
        _service.Get(key, () => { return "fakeValue"; }, 1);

        // Act
        var result = await _service.SetAsync(key, acquireMock.Object, cacheTime);

        // Assert
        acquireMock.Verify(a => a.Invoke(), Times.Once);
        var cacheResult = _memoryCache.Get(key);
        Assert.IsNotNull(cacheResult);
        Assert.AreEqual(cacheEntry, cacheResult);
    }
}