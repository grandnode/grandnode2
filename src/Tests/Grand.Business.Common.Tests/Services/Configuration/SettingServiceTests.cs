using Grand.Business.Common.Services.Configuration;
using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Configuration;

[TestClass]
public class SettingServiceTests
{
    private Mock<ICacheBase> _cacheMock;
    private Mock<IRepository<Setting>> _repositoryMock;
    private SettingService _service;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _repositoryMock = new Mock<IRepository<Setting>>();
        _service = new SettingService(_cacheMock.Object, _repositoryMock.Object);
    }

    [TestMethod]
    public async Task InsertSetting_InvokeExpectedMethods()
    {
        await _service.InsertSetting(new Setting());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Setting>()), Times.Once);
        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public void InsertSetting_NullArgument_TrhowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.InsertSetting(null));
    }

    [TestMethod]
    public async Task UpdateSetting_InvokeExpectedMethods()
    {
        await _service.UpdateSetting(new Setting());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Setting>()), Times.Once);
        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public void UpdateSetting_NullArgument_TrhowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.UpdateSetting(null));
    }

    [TestMethod]
    public async Task DeleteSetting_InvokeExpectedMethods()
    {
        await _service.DeleteSetting(new Setting());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Setting>()), Times.Once);
        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public void DeleteSetting_NullArgument_TrhowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.DeleteSetting(null));
    }

    [TestMethod]
    public async Task LoadSetting_StoreRowWinsOverGlobal()
    {
        var service = ServiceWithCache(Row("", "global"), Row("store1", "store"));

        Assert.AreEqual("store", (await service.LoadSetting<TestSettings>("store1")).Value);
        Assert.AreEqual("global", (await service.LoadSetting<TestSettings>("store2")).Value);
        Assert.AreEqual("global", (await service.LoadSetting<TestSettings>()).Value);
    }

    [TestMethod]
    public async Task LoadSetting_NoRow_ReturnsNewInstance()
    {
        var service = ServiceWithCache();

        var settings = await service.LoadSetting<TestSettings>("store1");

        Assert.IsNotNull(settings);
        Assert.IsNull(settings.Value);
    }

    [TestMethod]
    public async Task LoadSettingByType_AfterLoadStoreSettings_ReadsTheCachedStoreSettings()
    {
        var service = ServiceWithCache(Row("", "global"), Row("store1", "store"));

        await service.LoadStoreSettings("store1");
        var settings = (TestSettings)service.LoadSetting(typeof(TestSettings), "store1");
        var global = (TestSettings)service.LoadSetting(typeof(TestSettings), "");

        Assert.AreEqual("store", settings.Value);
        Assert.AreEqual("global", global.Value);
        //one query for the store, one for the global settings, none on resolving
        _repositoryMock.Verify(x => x.Table, Times.Exactly(2));
    }

    [TestMethod]
    public async Task LoadSettingByType_WithoutLoadedStoreSettings_ReadsTheStoreSettingsOnceForTheScope()
    {
        var service = ServiceWithCache(Row("", "global"), Row("store1", "store"), Row("", "other", "othersettings"));

        Assert.AreEqual("store", ((TestSettings)service.LoadSetting(typeof(TestSettings), "store1")).Value);
        Assert.AreEqual("other", ((OtherSettings)service.LoadSetting(typeof(OtherSettings), "store1")).Value);
        Assert.AreEqual("global", ((TestSettings)service.LoadSetting(typeof(TestSettings), "")).Value);
        _repositoryMock.Verify(x => x.Table, Times.Exactly(2));

        //what was read without the cache is not put into it
        await service.LoadSetting<TestSettings>("store1");
        _repositoryMock.Verify(x => x.Table, Times.Exactly(3));
    }

    [TestMethod]
    public async Task LoadSettingByType_AfterAWriteInTheScope_ReadsTheStoreSettingsAgain()
    {
        var row = Row("", "before");
        var service = ServiceWithCache(row);
        Assert.AreEqual("before", ((TestSettings)service.LoadSetting(typeof(TestSettings), "")).Value);

        row.Metadata = """{"Value":"after"}""";
        await service.UpdateSetting(row);

        Assert.AreEqual("after", ((TestSettings)service.LoadSetting(typeof(TestSettings), "")).Value);
    }

    [TestMethod]
    public async Task GetSettingByKey_ReturnsTheStoreRowOrTheDefaultValue()
    {
        var service = ServiceWithCache(Row("store1", "store", "my.key"));

        Assert.AreEqual("store", (await service.GetSettingByKey<TestSettings>("My.Key", storeId: "store1")).Value);
        var fallback = new TestSettings { Value = "default" };
        Assert.AreSame(fallback, await service.GetSettingByKey("missing", fallback, "store1"));
    }

    [TestMethod]
    public async Task ClearCache_ReadsTheSettingsAgain()
    {
        var row = Row("", "before");
        var service = ServiceWithCache(row);
        await service.LoadSetting<TestSettings>();

        row.Metadata = """{"Value":"after"}""";
        await service.ClearCache();

        Assert.AreEqual("after", (await service.LoadSetting<TestSettings>()).Value);
    }

    private SettingService ServiceWithCache(params Setting[] rows)
    {
        var table = rows.AsQueryable();
        _repositoryMock.Setup(x => x.Table).Returns(table);
        _repositoryMock.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Setting>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Setting> query, CancellationToken _) => query.ToList());

        var cache = new MemoryCacheBase(new MemoryCache(new MemoryCacheOptions()), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        return new SettingService(cache, _repositoryMock.Object);
    }

    private static Setting Row(string storeId, string value, string name = "testsettings")
    {
        return new Setting { Name = name, StoreId = storeId, Metadata = $$"""{"Value":"{{value}}"}""" };
    }

    public class TestSettings : ISettings
    {
        public string Value { get; set; }
    }

    public class OtherSettings : ISettings
    {
        public string Value { get; set; }
    }
}