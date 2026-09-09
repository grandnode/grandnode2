using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PickupPoint = Grand.Domain.Shipping.PickupPoint;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BasePickupPointControllerTests
{
    private Mock<IPickupPointService> _pickupPointServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;
    private Mock<ICountryService> _countryServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<PickupPointProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _pickupPointServiceMock = new Mock<IPickupPointService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();
        _warehouseServiceMock.Setup(w => w.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>());
        _countryServiceMock = new Mock<ICountryService>();
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country>());
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("msg");
    }

    private PickupPointController Build(IAdminDataScope<PickupPoint> scope) =>
        new(_pickupPointServiceMock.Object, _warehouseServiceMock.Object, _countryServiceMock.Object,
            _storeServiceMock.Object, _translationServiceMock.Object, scope)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

    [TestMethod]
    public async Task Create_GlobalScope_DoesNotForceStoreId()
    {
        var controller = Build(new GlobalAdminDataScope<PickupPoint>());
        PickupPoint inserted = null;
        _pickupPointServiceMock.Setup(s => s.InsertPickupPoint(It.IsAny<PickupPoint>()))
            .Callback<PickupPoint>(p => inserted = p).Returns(Task.CompletedTask);

        var model = new PickupPointModel { Name = "PP1", Address = new() };
        await controller.CreatePickupPoint(model, false);

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
        var scope = new StorePickupPointDataScope(contextAccessor.Object);
        var controller = Build(scope);

        PickupPoint inserted = null;
        _pickupPointServiceMock.Setup(s => s.InsertPickupPoint(It.IsAny<PickupPoint>()))
            .Callback<PickupPoint>(p => inserted = p).Returns(Task.CompletedTask);

        var model = new PickupPointModel { Name = "PP1", Address = new() };
        await controller.CreatePickupPoint(model, false);

        Assert.AreEqual("store-1", inserted!.StoreId);
    }

    [TestMethod]
    public async Task Edit_StoreScope_CrossStorePickupPoint_Denied()
    {
        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer)
            .Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        var scope = new StorePickupPointDataScope(contextAccessor.Object);
        var controller = Build(scope);

        _pickupPointServiceMock.Setup(s => s.GetPickupPointById("pp-2"))
            .ReturnsAsync(new PickupPoint { Id = "pp-2", StoreId = "store-2" });

        var result = await controller.EditPickupPoint("pp-2") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("PickupPoints", result.ActionName);
        _pickupPointServiceMock.Verify(s => s.UpdatePickupPoint(It.IsAny<PickupPoint>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateGet_StoreScope_WarehouseDropdownScopedToOwnStore()
    {
        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer)
            .Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        var scope = new StorePickupPointDataScope(contextAccessor.Object);
        var controller = Build(scope);

        await controller.CreatePickupPoint();

        _warehouseServiceMock.Verify(s => s.GetAllWarehouses("store-1", 0, int.MaxValue), Times.Once);
    }
}
