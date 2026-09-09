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
using ShippingMethod = Grand.Domain.Shipping.ShippingMethod;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseShippingMethodControllerTests
{
    private Mock<IShippingMethodService> _shippingMethodServiceMock;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<ShippingMethodProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _shippingMethodServiceMock = new Mock<IShippingMethodService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language>());
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("msg");
    }

    private ShippingMethodController Build(IAdminDataScope<ShippingMethod> scope) =>
        new(_shippingMethodServiceMock.Object, _languageServiceMock.Object, _storeServiceMock.Object,
            _translationServiceMock.Object, scope)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

    [TestMethod]
    public async Task Create_GlobalScope_DoesNotForceStoreId()
    {
        var controller = Build(new GlobalAdminDataScope<ShippingMethod>());
        ShippingMethod inserted = null;
        _shippingMethodServiceMock.Setup(s => s.InsertShippingMethod(It.IsAny<ShippingMethod>()))
            .Callback<ShippingMethod>(m => inserted = m).Returns(Task.CompletedTask);

        var model = new ShippingMethodModel { Name = "SM1" };
        await controller.CreateMethod(model, false);

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
        var scope = new StoreShippingMethodDataScope(contextAccessor.Object);
        var controller = Build(scope);

        ShippingMethod inserted = null;
        _shippingMethodServiceMock.Setup(s => s.InsertShippingMethod(It.IsAny<ShippingMethod>()))
            .Callback<ShippingMethod>(m => inserted = m).Returns(Task.CompletedTask);

        var model = new ShippingMethodModel { Name = "SM1" };
        await controller.CreateMethod(model, false);

        Assert.AreEqual("store-1", inserted!.StoreId);
    }

    [TestMethod]
    public async Task Edit_StoreScope_CrossStoreMethod_Denied()
    {
        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer)
            .Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        var scope = new StoreShippingMethodDataScope(contextAccessor.Object);
        var controller = Build(scope);

        _shippingMethodServiceMock.Setup(s => s.GetShippingMethodById("sm-2"))
            .ReturnsAsync(new ShippingMethod { Id = "sm-2", StoreId = "store-2" });

        var result = await controller.EditMethod("sm-2") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Methods", result.ActionName);
        _shippingMethodServiceMock.Verify(s => s.UpdateShippingMethod(It.IsAny<ShippingMethod>()), Times.Never);
    }
}
