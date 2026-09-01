using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Admin.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseContactAttributeControllerTests
{
    // BaseContactAttributeController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly.
    private class TestContactAttributeController(
        IContactAttributeViewModelService contactAttributeViewModelService,
        IContactAttributeService contactAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IAdminDataScope<ContactAttribute> scope)
        : BaseContactAttributeController(contactAttributeViewModelService, contactAttributeService,
            languageService, translationService, scope);

    private Mock<IContactAttributeViewModelService> _vmService = null!;
    private Mock<IContactAttributeService> _service = null!;
    private Mock<IAdminDataScope<ContactAttribute>> _scope = null!;
    private TestContactAttributeController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<ContactAttributeProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _vmService = new Mock<IContactAttributeViewModelService>();
        _service = new Mock<IContactAttributeService>();
        _scope = new Mock<IAdminDataScope<ContactAttribute>>();

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Grand.Domain.Localization.Language>());
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _sut = new TestContactAttributeController(
            _vmService.Object, _service.Object,
            languageServiceMock.Object, translationServiceMock.Object, _scope.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _sut.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task Edit_GlobalScope_CanViewAlwaysTrue()
    {
        var entity = new ContactAttribute { Id = "1" };
        _service.Setup(x => x.GetContactAttributeById("1")).ReturnsAsync(entity);
        _scope.Setup(x => x.CanView(entity)).ReturnsAsync(true);
        _vmService.Setup(x => x.PrepareConditionAttributes(It.IsAny<ContactAttributeModel>(), entity))
            .Returns(Task.CompletedTask);

        var result = await _sut.Edit("1") as ViewResult;

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task Edit_ScopeDeniesView_RedirectsToList()
    {
        var entity = new ContactAttribute { Id = "1" };
        _service.Setup(x => x.GetContactAttributeById("1")).ReturnsAsync(entity);
        _scope.Setup(x => x.CanView(entity)).ReturnsAsync(false);

        var result = await _sut.Edit("1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditNotList()
    {
        var entity = new ContactAttribute { Id = "1" };
        _service.Setup(x => x.GetContactAttributeById("1")).ReturnsAsync(entity);
        _scope.Setup(x => x.HasAccess(entity)).ReturnsAsync(false);

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
    }

    [TestMethod]
    public async Task ValueDelete_ScopeCanViewButDeniesAccess_ReturnsAccessDenied()
    {
        // Regression test for critical IDOR-class bug: ValueDelete (a mutation) must use HasAccess (strict)
        // not CanView (looser). A Store user viewing a global/multi-store attribute should NOT be able to
        // delete its values even though they can view the parent. This test proves the gap where CanView
        // and HasAccess diverge: CanView=true (can view), HasAccess=false (cannot mutate).
        var entity = new ContactAttribute { Id = "1" };
        _service.Setup(x => x.GetContactAttributeById("1")).ReturnsAsync(entity);
        _scope.Setup(x => x.CanView(entity)).ReturnsAsync(true);  // View allowed
        _scope.Setup(x => x.HasAccess(entity)).ReturnsAsync(false); // Mutation denied

        var result = await _sut.ValueDelete("value-1", "1") as JsonResult;

        Assert.IsNotNull(result);
        var data = result!.Value as DataSourceResult;
        Assert.AreEqual("Access denied", data!.Errors);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on thin subclasses.
/// Verifies that both Admin and Store ContactAttributeController subclasses carry
/// the required [AutoValidateAntiforgeryToken] and [AuthorizeMenu] attributes that
/// used to arrive transitively from BaseAdminController/BaseStoreController.
/// See Critical Finding 2 in Task 2 code review.
/// </summary>
[TestClass]
public class ContactAttributeControllerAttributeTests
{
    [TestMethod]
    public void AdminContactAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.ContactAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin ContactAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin ContactAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void AdminContactAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.ContactAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin ContactAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin ContactAttributeController missing [AuthorizeMenu]");
    }

    [TestMethod]
    public void StoreContactAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.ContactAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store ContactAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store ContactAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void StoreContactAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.ContactAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store ContactAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store ContactAttributeController missing [AuthorizeMenu]");
    }
}
