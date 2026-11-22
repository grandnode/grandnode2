using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Filters;

[TestClass]
public class StoreAccessAttributeTests
{
    private Mock<IRequestPermissionService> _permissionService;
    private StoreInformationSettings _storeInformationSettings;
    private bool _nextCalled;

    [TestInitialize]
    public void Setup()
    {
        _permissionService = new Mock<IRequestPermissionService>();
        _storeInformationSettings = new StoreInformationSettings();
        _nextCalled = false;
    }

    private class TestModel : IStoreAccessModel
    {
        public StoreAccess StoreAccess { get; set; }
    }

    private void Grant(string permissionSystemName, bool granted)
    {
        _permissionService.Setup(p => p.Authorize(It.Is<Permission>(x => x.SystemName == permissionSystemName)))
            .ReturnsAsync(granted);
    }

    private static ViewResult View(object model, string viewName = null)
    {
        return new ViewResult {
            ViewName = viewName,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
                Model = model
            }
        };
    }

    private async Task Execute(IActionResult result)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var executing = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, null);
        var filter = new StoreAccessAttribute.StoreAccessFilter(_permissionService.Object, _storeInformationSettings);

        await filter.OnResultExecutionAsync(executing, () =>
        {
            _nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), result, null));
        });
    }

    [TestMethod]
    public async Task OpenWhenStoreOpenAndNavigationAllowed()
    {
        Grant(PermissionSystemName.PublicStoreAllowNavigation, true);
        var model = new TestModel { StoreAccess = StoreAccess.Closed };

        await Execute(View(model));

        Assert.AreEqual(StoreAccess.Open, model.StoreAccess);
        Assert.IsTrue(_nextCalled);
    }

    [TestMethod]
    public async Task RegisteredOnlyWhenNavigationDenied()
    {
        Grant(PermissionSystemName.PublicStoreAllowNavigation, false);
        var model = new TestModel();

        await Execute(View(model));

        Assert.AreEqual(StoreAccess.RegisteredOnly, model.StoreAccess);
        Assert.IsTrue(_nextCalled);
    }

    [TestMethod]
    public async Task ClosedWhenStoreClosedAndNoClosedStoreAccess()
    {
        _storeInformationSettings.StoreClosed = true;
        Grant(PermissionSystemName.AccessClosedStore, false);
        Grant(PermissionSystemName.PublicStoreAllowNavigation, true);
        var model = new TestModel();

        await Execute(View(model));

        Assert.AreEqual(StoreAccess.Closed, model.StoreAccess);
        Assert.IsTrue(_nextCalled);
    }

    [TestMethod]
    public async Task RegisteredOnlyWhenStoreClosedButAccessGrantedAndNavigationDenied()
    {
        _storeInformationSettings.StoreClosed = true;
        Grant(PermissionSystemName.AccessClosedStore, true);
        Grant(PermissionSystemName.PublicStoreAllowNavigation, false);
        var model = new TestModel();

        await Execute(View(model));

        Assert.AreEqual(StoreAccess.RegisteredOnly, model.StoreAccess);
        Assert.IsTrue(_nextCalled);
    }

    [TestMethod]
    public async Task OverwritesAPostedValue()
    {
        Grant(PermissionSystemName.PublicStoreAllowNavigation, false);
        var model = new TestModel { StoreAccess = StoreAccess.Open };

        await Execute(View(model));

        Assert.AreEqual(StoreAccess.RegisteredOnly, model.StoreAccess);
        Assert.IsTrue(_nextCalled);
    }

    [TestMethod]
    public async Task SetsValueOnAnyViewResultModel()
    {
        //e.g. Register re-renders the "Login" view when registration is disabled
        Grant(PermissionSystemName.PublicStoreAllowNavigation, false);
        var model = new TestModel { StoreAccess = StoreAccess.Open };

        await Execute(View(model, "Login"));

        Assert.AreEqual(StoreAccess.RegisteredOnly, model.StoreAccess);
        Assert.IsTrue(_nextCalled);
    }

    [TestMethod]
    public async Task LeavesOtherModelsAlone()
    {
        await Execute(View(new object()));
        Assert.IsTrue(_nextCalled);

        _nextCalled = false;
        await Execute(new JsonResult(new TestModel()));
        Assert.IsTrue(_nextCalled);

        _permissionService.Verify(p => p.Authorize(It.IsAny<Permission>()), Times.Never);
    }
}
