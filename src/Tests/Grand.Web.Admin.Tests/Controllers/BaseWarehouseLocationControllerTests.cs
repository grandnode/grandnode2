using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain;
using Grand.Domain.Shipping;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseWarehouseLocationControllerTests
{
    private readonly Mock<IWarehouseLocationService> _locations = new();
    private readonly Mock<IWarehouseService> _warehouses = new();

    [TestInitialize]
    public void Setup()
    {
        _warehouses.Setup(x => x.GetWarehouseById("w1")).ReturnsAsync(new Warehouse { Id = "w1", StoreId = "store1" });
        _warehouses.Setup(x => x.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>(new List<Warehouse> { new() { Id = "w1", StoreId = "store1" } }, 0, 10));
        _locations.Setup(x => x.GetLocations(It.IsAny<IList<string>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<WarehouseLocation>(new List<WarehouseLocation> {
                new() { Id = "loc1", WarehouseId = "w1", Code = "A-01-03" }
            }, 0, 10));
    }

    private WarehouseLocationController Build(string storeId)
    {
        var scope = new Mock<IAdminDataScope<Warehouse>>();
        scope.Setup(x => x.GetDefaultStoreIdAsync()).ReturnsAsync(storeId);
        scope.Setup(x => x.HasAccess(It.IsAny<Warehouse>()))
            .ReturnsAsync((Warehouse warehouse) => storeId == null || warehouse.StoreId == storeId);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns("msg");
        return new WarehouseLocationController(_locations.Object, _warehouses.Object, translation.Object, scope.Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [TestMethod]
    public async Task Create_CodeTakenInTheWarehouse_Refused()
    {
        var controller = Build(null);

        var result = await controller.Create(new WarehouseLocationModel { WarehouseId = "w1", Code = " a-01-03 " }, false);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsFalse(controller.ModelState.IsValid);
        _locations.Verify(x => x.InsertLocation(It.IsAny<WarehouseLocation>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_SameLocationKeepsItsCode_Saved()
    {
        _locations.Setup(x => x.GetLocationById("loc1"))
            .ReturnsAsync(new WarehouseLocation { Id = "loc1", WarehouseId = "w1", Code = "A-01-03" });

        await Build(null).Edit(new WarehouseLocationModel { Id = "loc1", WarehouseId = "w1", Code = "A-01-03", Name = "x" }, false);

        _locations.Verify(x => x.UpdateLocation(It.Is<WarehouseLocation>(l => l.Name == "x")), Times.Once);
    }

    [TestMethod]
    public async Task Store_LocationOfAnotherStoresWarehouse_NotDeleted()
    {
        _locations.Setup(x => x.GetLocationById("loc1"))
            .ReturnsAsync(new WarehouseLocation { Id = "loc1", WarehouseId = "w1", Code = "A-01-03" });

        await Build("store2").Delete("loc1");

        _locations.Verify(x => x.DeleteLocation(It.IsAny<WarehouseLocation>()), Times.Never);
    }
}
