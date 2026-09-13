using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Warehouse = Grand.Domain.Shipping.Warehouse;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseWarehouseControllerTests
{
    private Mock<IWarehouseService> _warehouseServiceMock;
    private Mock<ICountryService> _countryServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _warehouseServiceMock = new Mock<IWarehouseService>();
        _countryServiceMock = new Mock<ICountryService>();
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country>());
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("msg");
    }

    private WarehouseController Build(IAdminDataScope<Warehouse> scope) =>
        new(_warehouseServiceMock.Object, _countryServiceMock.Object, _storeServiceMock.Object,
            _translationServiceMock.Object, scope)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

    [TestMethod]
    public async Task Create_GlobalScope_DoesNotForceStoreId()
    {
        var controller = Build(new GlobalAdminDataScope<Warehouse>());
        Warehouse inserted = null;
        _warehouseServiceMock.Setup(s => s.InsertWarehouse(It.IsAny<Warehouse>()))
            .Callback<Warehouse>(w => inserted = w).Returns(Task.CompletedTask);

        var model = new WarehouseModel { Name = "WH1", Address = new() };
        await controller.CreateWarehouse(model, false);

        Assert.AreEqual("", inserted!.StoreId ?? "");
    }

    [TestMethod]
    public async Task Create_StoreScope_ForcesStoreId()
    {
        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer)
            .Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        var scope = new StoreWarehouseDataScope(contextAccessor.Object);
        var controller = Build(scope);

        Warehouse inserted = null;
        _warehouseServiceMock.Setup(s => s.InsertWarehouse(It.IsAny<Warehouse>()))
            .Callback<Warehouse>(w => inserted = w).Returns(Task.CompletedTask);

        var model = new WarehouseModel { Name = "WH1", Address = new() };
        await controller.CreateWarehouse(model, false);

        Assert.AreEqual("store-1", inserted!.StoreId);
    }

    [TestMethod]
    public async Task Edit_StoreScope_CrossStoreWarehouse_Denied()
    {
        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer)
            .Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        var scope = new StoreWarehouseDataScope(contextAccessor.Object);
        var controller = Build(scope);

        _warehouseServiceMock.Setup(s => s.GetWarehouseById("wh-2"))
            .ReturnsAsync(new Warehouse { Id = "wh-2", StoreId = "store-2" });

        var result = await controller.EditWarehouse("wh-2") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Warehouses", result.ActionName);
        _warehouseServiceMock.Verify(s => s.UpdateWarehouse(It.IsAny<Warehouse>()), Times.Never);
    }
}
