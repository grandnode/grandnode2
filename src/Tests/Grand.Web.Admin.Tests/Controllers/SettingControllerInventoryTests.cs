using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class SettingControllerInventoryTests
{
    private SettingController _controller;
    private Mock<ISettingService> _settingService;

    [TestInitialize]
    public void Init()
    {
        AutoMapperConfig.Init(new MapperConfiguration(cfg => cfg.AddProfile<CatalogSettingsProfile>()));
        _settingService = new Mock<ISettingService>();
        _settingService.Setup(x => x.LoadSetting<CatalogSettings>(It.IsAny<string>()))
            .ReturnsAsync(new CatalogSettings());
        _settingService.Setup(x => x.LoadSetting<InventorySettings>(It.IsAny<string>()))
            .ReturnsAsync(new InventorySettings());

        //the admin works in the scope of one store
        var adminStoreService = new Mock<IAdminStoreService>();
        adminStoreService.Setup(x => x.GetActiveStore()).ReturnsAsync("store1");
        var services = new ServiceCollection().AddSingleton(adminStoreService.Object).BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());

        _controller = new SettingController(_settingService.Object, new Mock<IPictureService>().Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IMerchandiseReturnService>().Object,
            new Mock<ILanguageService>().Object, new Mock<ICacheBase>().Object,
            new Mock<IEnumTranslationService>().Object) {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object),
            Url = new Mock<IUrlHelper>().Object
        };
    }

    [TestMethod]
    public async Task Catalog_Post_WithStoreScope_SavesInventorySettingsGlobally()
    {
        var model = new CatalogSettingsModel {
            Inventory = { StockChangesOnlyThroughDocuments = true, ReceiptNumberPrefix = "GR" }
        };

        await _controller.Catalog(model);

        _settingService.Verify(x => x.SaveSetting(
            It.Is<InventorySettings>(s => s.StockChangesOnlyThroughDocuments && s.ReceiptNumberPrefix == "GR"), ""),
            Times.Once);
        _settingService.Verify(x => x.LoadSetting<InventorySettings>(""), Times.Once);
        _settingService.Verify(x => x.SaveSetting(It.IsAny<InventorySettings>(), "store1"), Times.Never);
    }

    [TestMethod]
    public async Task Catalog_Get_ShowsGlobalInventorySettings()
    {
        _settingService.Setup(x => x.LoadSetting<InventorySettings>(""))
            .ReturnsAsync(new InventorySettings { StockChangesOnlyThroughDocuments = true });

        var result = (ViewResult)await _controller.Catalog();

        Assert.IsTrue(((CatalogSettingsModel)result.Model!).Inventory.StockChangesOnlyThroughDocuments);
    }
}
