using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests;

[TestClass]
public class StoreContextSetterTests
{
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ISettingService> _settingServiceMock;
    private StoreContextSetter _setter;

    [TestInitialize]
    public void Setup()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _settingServiceMock = new Mock<ISettingService>();
        _setter = new StoreContextSetter(_storeServiceMock.Object, _settingServiceMock.Object, Mock.Of<IHttpContextAccessor>());
    }

    [TestMethod]
    public async Task InitializeStoreContext_LoadsTheSettingsOfTheStore()
    {
        //the settings injected for the store are resolved without a query only when read here first
        _storeServiceMock.Setup(s => s.GetStoreById("store1")).ReturnsAsync(new Store { Id = "store1" });

        var storeContext = await _setter.InitializeStoreContext("store1");

        Assert.AreEqual("store1", storeContext.CurrentStore.Id);
        _settingServiceMock.Verify(s => s.LoadStoreSettings("store1"), Times.Once);
    }

    [TestMethod]
    public async Task InitializeStoreContext_NoStore_LoadsNoSettings()
    {
        var storeContext = await _setter.InitializeStoreContext("missing");

        Assert.IsNull(storeContext.CurrentStore);
        _settingServiceMock.Verify(s => s.LoadStoreSettings(It.IsAny<string>()), Times.Never);
    }
}
