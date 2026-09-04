using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseMessageTemplateControllerTests
{
    private Mock<IMessageTemplateService> _messageTemplateService;
    private Mock<IEmailAccountService> _emailAccountService;
    private Mock<ILanguageService> _languageService;
    private Mock<ITranslationService> _translationService;
    private Mock<IMessageTokenProvider> _messageTokenProvider;
    private Mock<IDownloadService> _downloadService;
    private Mock<IAdminDataScope<MessageTemplate>> _scope;

    private class TestableMessageTemplateController(
        IMessageTemplateService messageTemplateService,
        IEmailAccountService emailAccountService,
        ILanguageService languageService,
        ITranslationService translationService,
        IMessageTokenProvider messageTokenProvider,
        IDownloadService downloadService,
        IAdminDataScope<MessageTemplate> scope)
        : BaseMessageTemplateController(messageTemplateService, emailAccountService, languageService,
            translationService, messageTokenProvider, downloadService, scope);

    private TestableMessageTemplateController CreateController()
    {
        var controller = new TestableMessageTemplateController(_messageTemplateService.Object, _emailAccountService.Object,
            _languageService.Object, _translationService.Object, _messageTokenProvider.Object, _downloadService.Object,
            _scope.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);

        return controller;
    }

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MessageTemplateProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _messageTemplateService = new Mock<IMessageTemplateService>();
        _emailAccountService = new Mock<IEmailAccountService>();
        _languageService = new Mock<ILanguageService>();
        _translationService = new Mock<ITranslationService>();
        _messageTokenProvider = new Mock<IMessageTokenProvider>();
        _downloadService = new Mock<IDownloadService>();
        _scope = new Mock<IAdminDataScope<MessageTemplate>>();

        _messageTokenProvider.Setup(s => s.GetListOfAllowedTokens()).Returns(Array.Empty<string>());
        _emailAccountService.Setup(s => s.GetAllEmailAccounts(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Grand.Domain.Messages.EmailAccount>(new List<Grand.Domain.Messages.EmailAccount>(), 0, int.MaxValue));
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns((string k) => k);
        _languageService.Setup(s => s.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(new List<Grand.Domain.Localization.Language>());
    }

    [TestMethod]
    public async Task EditGet_NotFound_RedirectsToList()
    {
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("missing")).ReturnsAsync((MessageTemplate)null);

        var result = await CreateController().Edit("missing") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditGet_CanViewFalse_RedirectsToList()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-2"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.CanView(template)).ReturnsAsync(false);

        var result = await CreateController().Edit("mt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditGet_GlobalTemplate_Store_IsReadOnlyTrue_CanCopyTrue()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = false, Stores = [] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.CanView(template)).ReturnsAsync(true);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(false);
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");

        var result = await CreateController().Edit("mt-1") as ViewResult;
        var model = result?.Model as MessageTemplateModel;

        Assert.IsNotNull(model);
        Assert.IsTrue(model.IsReadOnly);
        Assert.IsTrue(model.CanCopy);
    }

    [TestMethod]
    public async Task EditGet_OwnStoreTemplate_Store_IsReadOnlyFalse_CanCopyFalse()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-1"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.CanView(template)).ReturnsAsync(true);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");

        var result = await CreateController().Edit("mt-1") as ViewResult;
        var model = result?.Model as MessageTemplateModel;

        Assert.IsNotNull(model);
        Assert.IsFalse(model.IsReadOnly);
        Assert.IsFalse(model.CanCopy);
    }

    [TestMethod]
    public async Task EditGet_Admin_AlwaysNotReadOnly_AlwaysCanCopy()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-2"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.CanView(template)).ReturnsAsync(true);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);

        var result = await CreateController().Edit("mt-1") as ViewResult;
        var model = result?.Model as MessageTemplateModel;

        Assert.IsNotNull(model);
        Assert.IsFalse(model.IsReadOnly);
        Assert.IsTrue(model.CanCopy);
    }

    [TestMethod]
    public async Task EditPost_HasAccessFalse_RedirectsToListWithoutSaving()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-2"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(false);

        var result = await CreateController().Edit(new MessageTemplateModel { Id = "mt-1" }, false) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _messageTemplateService.Verify(s => s.UpdateMessageTemplate(It.IsAny<MessageTemplate>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_Store_ForcesLimitedToStoresRegardlessOfSubmittedModel()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-1"], Name = "N" };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");

        MessageTemplate saved = null;
        _messageTemplateService.Setup(s => s.UpdateMessageTemplate(It.IsAny<MessageTemplate>()))
            .Callback<MessageTemplate>(t => saved = t)
            .Returns(Task.CompletedTask);

        var model = new MessageTemplateModel { Id = "mt-1", Name = "N", Stores = [] };
        await CreateController().Edit(model, false);

        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.LimitedToStores);
        CollectionAssert.AreEqual(new[] { "store-1" }, saved.Stores.ToArray());
    }

    [TestMethod]
    public async Task EditPost_Admin_DoesNotForceLimitedToStores()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = false, Stores = [], Name = "N" };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);

        MessageTemplate saved = null;
        _messageTemplateService.Setup(s => s.UpdateMessageTemplate(It.IsAny<MessageTemplate>()))
            .Callback<MessageTemplate>(t => saved = t)
            .Returns(Task.CompletedTask);

        var model = new MessageTemplateModel { Id = "mt-1", Name = "N" };
        await CreateController().Edit(model, false);

        Assert.IsNotNull(saved);
        Assert.IsFalse(saved.LimitedToStores);
    }

    [TestMethod]
    public async Task Delete_HasAccessFalse_DoesNotDelete()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-2"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(false);

        var result = await CreateController().Delete("mt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _messageTemplateService.Verify(s => s.DeleteMessageTemplate(It.IsAny<MessageTemplate>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_HasAccessTrue_Deletes()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-1"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(true);

        var result = await CreateController().Delete("mt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _messageTemplateService.Verify(s => s.DeleteMessageTemplate(template), Times.Once);
    }

    [TestMethod]
    public async Task CopyTemplate_Store_OwnTemplate_Denied()
    {
        var template = new MessageTemplate { Id = "mt-1", LimitedToStores = true, Stores = ["store-1"] };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");

        var result = await CreateController().CopyTemplate(new MessageTemplateModel { Id = "mt-1" }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _messageTemplateService.Verify(s => s.CopyMessageTemplate(It.IsAny<MessageTemplate>()), Times.Never);
    }

    [TestMethod]
    public async Task CopyTemplate_Store_GlobalTemplate_CopiesAndAssignsToCurrentStore()
    {
        var template = new MessageTemplate { Id = "mt-1", Name = "N", LimitedToStores = false, Stores = [] };
        var copy = new MessageTemplate { Id = "mt-2", Name = "N" };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.HasAccess(template)).ReturnsAsync(false);
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        _messageTemplateService.Setup(s => s.GetAllMessageTemplates("", "N", 0, int.MaxValue)).ReturnsAsync(new PagedList<MessageTemplate>(new List<MessageTemplate>(), 0, int.MaxValue));
        _messageTemplateService.Setup(s => s.CopyMessageTemplate(template)).ReturnsAsync(copy);

        MessageTemplate updated = null;
        _messageTemplateService.Setup(s => s.UpdateMessageTemplate(It.IsAny<MessageTemplate>()))
            .Callback<MessageTemplate>(t => updated = t)
            .Returns(Task.CompletedTask);

        var result = await CreateController().CopyTemplate(new MessageTemplateModel { Id = "mt-1" }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.LimitedToStores);
        CollectionAssert.AreEqual(new[] { "store-1" }, updated.Stores.ToArray());
    }

    [TestMethod]
    public async Task CopyTemplate_Admin_Unrestricted()
    {
        var template = new MessageTemplate { Id = "mt-1", Name = "N", LimitedToStores = true, Stores = ["store-1"] };
        var copy = new MessageTemplate { Id = "mt-2", Name = "N" };
        _messageTemplateService.Setup(s => s.GetMessageTemplateById("mt-1")).ReturnsAsync(template);
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);
        _messageTemplateService.Setup(s => s.CopyMessageTemplate(template)).ReturnsAsync(copy);

        var result = await CreateController().CopyTemplate(new MessageTemplateModel { Id = "mt-1" }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        _messageTemplateService.Verify(s => s.UpdateMessageTemplate(It.IsAny<MessageTemplate>()), Times.Never);
    }
}
