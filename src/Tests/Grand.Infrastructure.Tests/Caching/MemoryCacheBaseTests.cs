using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Caching.Tests
{
    [TestClass()]
    public class MemoryCacheBaseTests
    {
        private Mock<IMediator> _mediatorMock;
        private MemoryCacheBase _service;
        private IMemoryCache _memoryCache;

        [TestInitialize]
        public void Init()
        {
            CommonHelper.CacheTimeMinutes = 1;

            var services = new ServiceCollection();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();

            _memoryCache = serviceProvider.GetService<IMemoryCache>();
            _mediatorMock = new Mock<IMediator>();
            _service = new MemoryCacheBase(_memoryCache, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task GetAsyncTest()
        {
            CommonHelper.CacheTimeMinutes = 10;
            var result = await _service.GetAsync<string>("key", () => { return Task.FromResult("test"); });
            Assert.AreEqual(result, "test");
        }

        [TestMethod()]
        public async Task GetAsyncTest_CacheTimeMinutes()
        {
            var result = await _service.GetAsync<string>("key", () => { return Task.FromResult("test"); }, 1);
            Assert.AreEqual(result, "test");
        }

        [TestMethod()]
        public void GetTest()
        {
            var result = _service.Get<string>("key", () => { return "test"; });
            Assert.AreEqual(result, "test");
        }

        [TestMethod()]
        public void GetTest_CacheTimeMinutes()
        {
            var result = _service.Get<string>("key", () => { return "test"; }, 1);
            Assert.AreEqual(result, "test");
        }

        [TestMethod()]
        public async Task RemoveAsyncTest_IsNull()
        {
            _service.Get<string>("key", () => { return "test"; }, 1);
            await _service.RemoveAsync("key");
            var result = _memoryCache.Get("key");
            Assert.IsNull(result);
        }

        [TestMethod()]
        public async Task RemoveAsyncTest_NotNull()
        {
            _service.Get<string>("key", () => { return "test"; }, 1);
            await _service.RemoveAsync("key1");
            var result = _memoryCache.Get("key");
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task RemoveByPrefixTest()
        {
            _service.Get<string>("key1", () => { return "test"; }, 1);
            _service.Get<string>("key2", () => { return "test"; }, 1);
            _service.Get<string>("test", () => { return "test"; }, 1);
            await _service.RemoveByPrefix("key");
            var result = _memoryCache.Get("key1");
            Assert.IsNull(result);
        }

        [TestMethod()]
        public async Task ClearTest()
        {
            _service.Get<string>("key1", () => { return "test"; }, 1);
            _service.Get<string>("key2", () => { return "test"; }, 1);
            _service.Get<string>("test", () => { return "test"; }, 1);
            await _service.Clear();
            var result = _memoryCache.Get("key1");
            Assert.IsNull(result);
        }
    }
}