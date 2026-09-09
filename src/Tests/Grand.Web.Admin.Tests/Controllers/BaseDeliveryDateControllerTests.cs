using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Localization;
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
using DeliveryDate = Grand.Domain.Shipping.DeliveryDate;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseDeliveryDateControllerTests
{
    private Mock<IDeliveryDateService> _deliveryDateServiceMock;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<DeliveryDateProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _deliveryDateServiceMock = new Mock<IDeliveryDateService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language>());
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("msg");
    }

    private DeliveryDateController Build(IAdminDataScope<DeliveryDate> scope) =>
        new(_deliveryDateServiceMock.Object, _languageServiceMock.Object, _storeServiceMock.Object,
            _translationServiceMock.Object, scope)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

    [TestMethod]
    public async Task Create_GlobalScope_DoesNotForceStoreId()
    {
        var controller = Build(new GlobalAdminDataScope<DeliveryDate>());
        DeliveryDate inserted = null;
        _deliveryDateServiceMock.Setup(s => s.InsertDeliveryDate(It.IsAny<DeliveryDate>()))
            .Callback<DeliveryDate>(m => inserted = m).Returns(Task.CompletedTask);

        var model = new DeliveryDateModel { Name = "DD1" };
        await controller.CreateDeliveryDate(model, false);

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
        var scope = new StoreDeliveryDateDataScope(contextAccessor.Object);
        var controller = Build(scope);

        DeliveryDate inserted = null;
        _deliveryDateServiceMock.Setup(s => s.InsertDeliveryDate(It.IsAny<DeliveryDate>()))
            .Callback<DeliveryDate>(m => inserted = m).Returns(Task.CompletedTask);

        var model = new DeliveryDateModel { Name = "DD1" };
        await controller.CreateDeliveryDate(model, false);

        Assert.AreEqual("store-1", inserted!.StoreId);
    }

    [TestMethod]
    public async Task Edit_StoreScope_CrossStoreDeliveryDate_Denied()
    {
        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer)
            .Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        var scope = new StoreDeliveryDateDataScope(contextAccessor.Object);
        var controller = Build(scope);

        _deliveryDateServiceMock.Setup(s => s.GetDeliveryDateById("dd-2"))
            .ReturnsAsync(new DeliveryDate { Id = "dd-2", StoreId = "store-2" });

        var result = await controller.EditDeliveryDate("dd-2") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("DeliveryDates", result.ActionName);
        _deliveryDateServiceMock.Verify(s => s.UpdateDeliveryDate(It.IsAny<DeliveryDate>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateDeliveryDate_Get_DefaultsColorSquaresRgb()
    {
        var controller = Build(new GlobalAdminDataScope<DeliveryDate>());

        var result = await controller.CreateDeliveryDate() as ViewResult;

        var model = result!.Model as DeliveryDateModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("#000000", model.ColorSquaresRgb);
    }

    [TestMethod]
    public async Task EditDeliveryDate_Get_FillsBlankColorSquaresRgb_WithDefault()
    {
        var controller = Build(new GlobalAdminDataScope<DeliveryDate>());
        _deliveryDateServiceMock.Setup(s => s.GetDeliveryDateById("dd-1"))
            .ReturnsAsync(new DeliveryDate { Id = "dd-1", ColorSquaresRgb = "" });

        var result = await controller.EditDeliveryDate("dd-1") as ViewResult;

        var model = result!.Model as DeliveryDateModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("#000000", model.ColorSquaresRgb);
    }

    [TestMethod]
    public async Task EditDeliveryDate_Get_PreservesExistingColorSquaresRgb()
    {
        var controller = Build(new GlobalAdminDataScope<DeliveryDate>());
        _deliveryDateServiceMock.Setup(s => s.GetDeliveryDateById("dd-1"))
            .ReturnsAsync(new DeliveryDate { Id = "dd-1", ColorSquaresRgb = "#ff0000" });

        var result = await controller.EditDeliveryDate("dd-1") as ViewResult;

        var model = result!.Model as DeliveryDateModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("#ff0000", model.ColorSquaresRgb);
    }
}
