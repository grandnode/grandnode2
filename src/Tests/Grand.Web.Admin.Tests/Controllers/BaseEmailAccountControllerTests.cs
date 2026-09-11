#nullable enable

using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseEmailAccountControllerTests
{
    // Concrete subclass exists only to instantiate the abstract base for direct-call unit tests -
    // same pattern as BaseMessageTemplateControllerTests/BaseCategoryControllerTests.
    private class TestEmailAccountController(
        IEmailAccountViewModelService emailAccountViewModelService,
        IEmailAccountService emailAccountService,
        ITranslationService translationService,
        IAdminDataScope<EmailAccount> scope)
        : BaseEmailAccountController(emailAccountViewModelService, emailAccountService, translationService, scope);

    private Mock<IEmailAccountViewModelService> _emailAccountViewModelServiceMock = null!;
    private Mock<IEmailAccountService> _emailAccountServiceMock = null!;
    private Mock<ITranslationService> _translationServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<EmailAccountProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _emailAccountViewModelServiceMock = new Mock<IEmailAccountViewModelService>();
        _emailAccountServiceMock = new Mock<IEmailAccountService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string s) => s);
    }

    private TestEmailAccountController CreateController(IAdminDataScope<EmailAccount> scope)
    {
        var controller = new TestEmailAccountController(
            _emailAccountViewModelServiceMock.Object,
            _emailAccountServiceMock.Object,
            _translationServiceMock.Object,
            scope);

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
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static Mock<IAdminDataScope<EmailAccount>> AdminScope()
    {
        var scope = new Mock<IAdminDataScope<EmailAccount>>();
        scope.Setup(s => s.DefaultStoreId).Returns((string?)null);
        scope.Setup(s => s.HasAccess(It.IsAny<EmailAccount>())).ReturnsAsync(true);
        scope.Setup(s => s.ShowStoreSelector).Returns(true);
        return scope;
    }

    private static Mock<IAdminDataScope<EmailAccount>> StoreScope(string storeId, bool hasAccess)
    {
        var scope = new Mock<IAdminDataScope<EmailAccount>>();
        scope.Setup(s => s.DefaultStoreId).Returns(storeId);
        scope.Setup(s => s.HasAccess(It.IsAny<EmailAccount>())).ReturnsAsync(hasAccess);
        scope.Setup(s => s.ShowStoreSelector).Returns(false);
        return scope;
    }

    // --- Create GET ---

    [TestMethod]
    public async Task CreateGet_AdminScope_DoesNotForceStoreId()
    {
        _emailAccountViewModelServiceMock.Setup(s => s.PrepareEmailAccountModel())
            .ReturnsAsync(new EmailAccountModel { StoreId = "" });
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Create() as ViewResult;

        var model = (EmailAccountModel)result!.Model!;
        Assert.AreEqual("", model.StoreId);
    }

    [TestMethod]
    public async Task CreateGet_StoreScope_ForcesStoreId()
    {
        _emailAccountViewModelServiceMock.Setup(s => s.PrepareEmailAccountModel())
            .ReturnsAsync(new EmailAccountModel { StoreId = "" });
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Create() as ViewResult;

        var model = (EmailAccountModel)result!.Model!;
        Assert.AreEqual("store-1", model.StoreId);
    }

    [TestMethod]
    public async Task CreateGet_AdminScope_KeepsAvailableStores()
    {
        _emailAccountViewModelServiceMock.Setup(s => s.PrepareEmailAccountModel())
            .ReturnsAsync(new EmailAccountModel {
                AvailableStores = { new SelectListItem { Value = "store-1", Text = "Store 1" } }
            });
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Create() as ViewResult;

        var model = (EmailAccountModel)result!.Model!;
        Assert.AreEqual(1, model.AvailableStores.Count);
    }

    [TestMethod]
    public async Task CreateGet_StoreScope_ClearsAvailableStores()
    {
        // Regression test for the latent cross-store leak flagged in code review: Store's own
        // view never renders AvailableStores (it emits only a hidden StoreId input), but the
        // shared PrepareEmailAccountModel() call always populates it - every store's id/name must
        // not reach a model handed to Store's widget zones via additional-data="Model".
        _emailAccountViewModelServiceMock.Setup(s => s.PrepareEmailAccountModel())
            .ReturnsAsync(new EmailAccountModel {
                AvailableStores = { new SelectListItem { Value = "store-1", Text = "Store 1" } }
            });
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Create() as ViewResult;

        var model = (EmailAccountModel)result!.Model!;
        Assert.AreEqual(0, model.AvailableStores.Count);
    }

    // --- Create POST ---

    [TestMethod]
    public async Task CreatePost_StoreScope_ForcesStoreIdBeforeInsert()
    {
        EmailAccountModel? insertedModel = null;
        _emailAccountViewModelServiceMock.Setup(s => s.InsertEmailAccountModel(It.IsAny<EmailAccountModel>()))
            .Callback<EmailAccountModel>(m => insertedModel = m)
            .ReturnsAsync(new EmailAccount { Id = "new-id" });
        var controller = CreateController(StoreScope("store-1", true).Object);
        var model = new EmailAccountModel { StoreId = "some-other-store" };

        await controller.Create(model, false);

        Assert.AreEqual("store-1", insertedModel!.StoreId);
    }

    [TestMethod]
    public async Task CreatePost_AdminScope_LeavesPostedStoreIdUntouched()
    {
        EmailAccountModel? insertedModel = null;
        _emailAccountViewModelServiceMock.Setup(s => s.InsertEmailAccountModel(It.IsAny<EmailAccountModel>()))
            .Callback<EmailAccountModel>(m => insertedModel = m)
            .ReturnsAsync(new EmailAccount { Id = "new-id" });
        var controller = CreateController(AdminScope().Object);
        var model = new EmailAccountModel { StoreId = "whatever-was-posted" };

        await controller.Create(model, false);

        Assert.AreEqual("whatever-was-posted", insertedModel!.StoreId);
    }

    // --- Edit GET ---

    [TestMethod]
    public async Task EditGet_NotFound_RedirectsToList()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("missing")).ReturnsAsync((EmailAccount?)null);
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Edit("missing") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task EditGet_CrossStoreDenied_RedirectsToList()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-2" });
        var controller = CreateController(StoreScope("store-1", false).Object);

        var result = await controller.Edit("acc-1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task EditGet_OwnedByCurrentStore_ReturnsView()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-1", Email = "a@b.com" });
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Edit("acc-1") as ViewResult;

        Assert.IsNotNull(result);
        _emailAccountViewModelServiceMock.Verify(s => s.PrepareAvailableStores(It.IsAny<EmailAccountModel>()), Times.Once);
    }

    [TestMethod]
    public async Task EditGet_StoreScope_ClearsAvailableStores()
    {
        // Same leak as CreateGet_StoreScope_ClearsAvailableStores, via PrepareAvailableStores
        // instead of PrepareEmailAccountModel.
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-1", Email = "a@b.com" });
        _emailAccountViewModelServiceMock
            .Setup(s => s.PrepareAvailableStores(It.IsAny<EmailAccountModel>()))
            .Callback<EmailAccountModel>(m => m.AvailableStores.Add(new SelectListItem { Value = "store-1", Text = "Store 1" }))
            .Returns(Task.CompletedTask);
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Edit("acc-1") as ViewResult;

        var model = (EmailAccountModel)result!.Model!;
        Assert.AreEqual(0, model.AvailableStores.Count);
    }

    [TestMethod]
    public async Task EditGet_AdminScope_KeepsAvailableStores()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-1", Email = "a@b.com" });
        _emailAccountViewModelServiceMock
            .Setup(s => s.PrepareAvailableStores(It.IsAny<EmailAccountModel>()))
            .Callback<EmailAccountModel>(m => m.AvailableStores.Add(new SelectListItem { Value = "store-1", Text = "Store 1" }))
            .Returns(Task.CompletedTask);
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Edit("acc-1") as ViewResult;

        var model = (EmailAccountModel)result!.Model!;
        Assert.AreEqual(1, model.AvailableStores.Count);
    }

    // --- Edit POST ---

    [TestMethod]
    public async Task EditPost_StoreScope_PreventsMovingToAnotherStore()
    {
        var existing = new EmailAccount { Id = "acc-1", StoreId = "store-1" };
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1")).ReturnsAsync(existing);
        EmailAccountModel? updatedModel = null;
        _emailAccountViewModelServiceMock
            .Setup(s => s.UpdateEmailAccountModel(existing, It.IsAny<EmailAccountModel>()))
            .Callback<EmailAccount, EmailAccountModel>((_, m) => updatedModel = m)
            .ReturnsAsync(existing);
        var controller = CreateController(StoreScope("store-1", true).Object);
        var model = new EmailAccountModel { Id = "acc-1", StoreId = "store-2" };

        await controller.Edit(model, false);

        Assert.AreEqual("store-1", updatedModel!.StoreId);
    }

    [TestMethod]
    public async Task EditPost_CrossStoreDenied_RedirectsToList_NoUpdateCalled()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-2" });
        var controller = CreateController(StoreScope("store-1", false).Object);

        var result = await controller.Edit(new EmailAccountModel { Id = "acc-1" }, false) as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _emailAccountViewModelServiceMock.Verify(
            s => s.UpdateEmailAccountModel(It.IsAny<EmailAccount>(), It.IsAny<EmailAccountModel>()), Times.Never);
    }

    // --- SendTestEmail ---

    [TestMethod]
    public async Task SendTestEmail_CrossStoreDenied_RedirectsToList()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-2" });
        var controller = CreateController(StoreScope("store-1", false).Object);

        var result = await controller.SendTestEmail(new EmailAccountModel { Id = "acc-1" }) as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _emailAccountViewModelServiceMock.Verify(
            s => s.SendTestEmail(It.IsAny<EmailAccount>(), It.IsAny<EmailAccountModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SendTestEmail_Owned_CallsService()
    {
        var account = new EmailAccount { Id = "acc-1", StoreId = "store-1" };
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1")).ReturnsAsync(account);
        var controller = CreateController(StoreScope("store-1", true).Object);

        await controller.SendTestEmail(new EmailAccountModel { Id = "acc-1", SendTestEmailTo = "x@y.com" });

        _emailAccountViewModelServiceMock.Verify(s => s.SendTestEmail(account, It.IsAny<EmailAccountModel>()), Times.Once);
    }

    // --- Delete ---

    [TestMethod]
    public async Task Delete_CrossStoreDenied_RedirectsToList_NoDeleteCalled()
    {
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1"))
            .ReturnsAsync(new EmailAccount { Id = "acc-1", StoreId = "store-2" });
        var controller = CreateController(StoreScope("store-1", false).Object);

        var result = await controller.Delete("acc-1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _emailAccountServiceMock.Verify(s => s.DeleteEmailAccount(It.IsAny<EmailAccount>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_Owned_CallsServiceAndRedirectsToList()
    {
        var account = new EmailAccount { Id = "acc-1", StoreId = "store-1" };
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1")).ReturnsAsync(account);
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Delete("acc-1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _emailAccountServiceMock.Verify(s => s.DeleteEmailAccount(account), Times.Once);
    }

    [TestMethod]
    public async Task Delete_ServiceThrowsNonGrandException_CaughtAndRedirectsToEdit()
    {
        // Regression test for the deliberate catch(Exception) widening: a plain
        // InvalidOperationException (not a GrandException) must be caught here, not propagate as
        // an unhandled 500 the way it would have on Store's original narrower catch.
        var account = new EmailAccount { Id = "acc-1", StoreId = "store-1" };
        _emailAccountServiceMock.Setup(s => s.GetEmailAccountById("acc-1")).ReturnsAsync(account);
        _emailAccountServiceMock.Setup(s => s.DeleteEmailAccount(account))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Delete("acc-1") as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
    }
}
