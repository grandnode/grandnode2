using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Web.Common.Security.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Authorization;

[TestClass]
public class StoreAccessServiceTests
{
    private Mock<IRequestPermissionService> _permissionServiceMock;
    private StoreInformationSettings _storeInformationSettings;
    private StoreAccessService _service;

    [TestInitialize]
    public void Init()
    {
        _permissionServiceMock = new Mock<IRequestPermissionService>();
        _storeInformationSettings = new StoreInformationSettings();
        _service = new StoreAccessService(_permissionServiceMock.Object, _storeInformationSettings);
    }

    private void Grant(string permissionSystemName, bool granted)
    {
        _permissionServiceMock.Setup(p => p.Authorize(It.Is<Permission>(x => x.SystemName == permissionSystemName)))
            .ReturnsAsync(granted);
    }

    [TestMethod]
    public async Task OpenWhenStoreOpenAndNavigationAllowed()
    {
        Grant(PermissionSystemName.PublicStoreAllowNavigation, true);

        Assert.AreEqual(StoreAccess.Open, await _service.GetStoreAccess());
        _permissionServiceMock.Verify(
            p => p.Authorize(It.Is<Permission>(x => x.SystemName == PermissionSystemName.AccessClosedStore)),
            Times.Never);
    }

    [TestMethod]
    public async Task RegisteredOnlyWhenNavigationDenied()
    {
        Grant(PermissionSystemName.PublicStoreAllowNavigation, false);

        Assert.AreEqual(StoreAccess.RegisteredOnly, await _service.GetStoreAccess());
    }

    [TestMethod]
    public async Task ClosedWhenStoreClosedAndNoClosedStoreAccess()
    {
        _storeInformationSettings.StoreClosed = true;
        Grant(PermissionSystemName.AccessClosedStore, false);
        Grant(PermissionSystemName.PublicStoreAllowNavigation, true);

        Assert.AreEqual(StoreAccess.Closed, await _service.GetStoreAccess());
    }

    [TestMethod]
    public async Task RegisteredOnlyWhenStoreClosedButAccessGrantedAndNavigationDenied()
    {
        _storeInformationSettings.StoreClosed = true;
        Grant(PermissionSystemName.AccessClosedStore, true);
        Grant(PermissionSystemName.PublicStoreAllowNavigation, false);

        Assert.AreEqual(StoreAccess.RegisteredOnly, await _service.GetStoreAccess());
    }
}
