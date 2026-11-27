using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.PushNotifications;
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class SettingControllerPushNotificationsTests
{
    private const string Template = "var config = {\napiKey: \"\",\nappId: \"\",\nauthDomain: \"\",\ndatabaseURL: \"\",\nprojectId: \"\",\nstorageBucket: \"\",\nmessagingSenderId: \"\",\n};";
    private SettingController _controller;
    private Mock<IMediaFileStore> _fileStore;
    private string _written;

    [TestInitialize]
    public void Init()
    {
        AutoMapperConfig.Init(new MapperConfiguration(cfg => cfg.AddProfile<PushNotificationsSettingsProfile>()));
        var settingService = new Mock<ISettingService>();
        settingService.Setup(x => x.LoadSetting<PushNotificationsSettings>(It.IsAny<string>()))
            .ReturnsAsync(new PushNotificationsSettings());

        _fileStore = new Mock<IMediaFileStore>();
        _fileStore.Setup(x => x.GetFileInfo("firebase-messaging-sw.js")).ReturnsAsync(new Mock<IFileStoreEntry>().Object);
        _fileStore.Setup(x => x.ReadAllText("firebase-messaging-sw.js")).ReturnsAsync(() => _written ?? Template);
        _fileStore.Setup(x => x.WriteAllText("firebase-messaging-sw.js", It.IsAny<string>()))
            .Callback<string, string>((_, text) => _written = text)
            .Returns(Task.CompletedTask);

        var adminStoreService = new Mock<IAdminStoreService>();
        adminStoreService.Setup(x => x.GetActiveStore()).ReturnsAsync("");
        var services = new ServiceCollection().AddSingleton(adminStoreService.Object).BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };

        _controller = new SettingController(settingService.Object, new Mock<IPictureService>().Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IMerchandiseReturnService>().Object,
            new Mock<ILanguageService>().Object, new Mock<ICacheBase>().Object,
            new Mock<IEnumTranslationService>().Object) {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object),
            Url = new Mock<IUrlHelper>().Object
        };
    }

    [TestMethod]
    public async Task PushNotifications_Post_WritesValuesAsJavaScriptStrings()
    {
        var model = new PushNotificationsSettingsModel {
            PushApiKey = "key\"; alert(1); \"", AuthDomain = "back\\slash", ProjectId = "line\nbreak", AppId = "app-1"
        };

        await _controller.PushNotifications(model, _fileStore.Object);

        StringAssert.Contains(_written, "apiKey: \"key\\u0022; alert(1); \\u0022\",");
        StringAssert.Contains(_written, "authDomain: \"back\\\\slash\",");
        StringAssert.Contains(_written, "projectId: \"line\\nbreak\",");
        StringAssert.Contains(_written, "appId: \"app-1\",");
    }

    [TestMethod]
    public async Task PushNotifications_Post_ValueNamingAnotherKey_ReplacesOnlyItsOwnLine()
    {
        await _controller.PushNotifications(new PushNotificationsSettingsModel { PushApiKey = "appId", AppId = "first" },
            _fileStore.Object);

        //saved again: the apiKey line now mentions appId, it must stay the apiKey line
        await _controller.PushNotifications(new PushNotificationsSettingsModel { PushApiKey = "appId", AppId = "second" },
            _fileStore.Object);

        StringAssert.Contains(_written, "apiKey: \"appId\",");
        StringAssert.Contains(_written, "appId: \"second\",");
    }
}
