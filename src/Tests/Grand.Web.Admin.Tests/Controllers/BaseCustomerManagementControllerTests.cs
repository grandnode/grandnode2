using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerManagementControllerTests
{
    private class TestManagementController(
        ICustomerService customerService, ICustomerViewModelService customerViewModelService,
        ICustomerManagerService customerManagerService, ICustomerAttributeParser customerAttributeParser,
        ICustomerAttributeService customerAttributeService, ICustomerMessageProviderService customerMessageProviderService,
        IGroupService groupService, IRequestTranslationService translationService, IContextAccessor contextAccessor,
        CustomerSettings customerSettings, IAdminDataScope<Customer> scope,
        IRequestPermissionService permissionService, IExportManager<Customer> exportManager)
        : BaseCustomerManagementController(customerService, customerViewModelService, customerManagerService,
            customerAttributeParser, customerAttributeService, customerMessageProviderService, groupService,
            translationService, contextAccessor, customerSettings, scope, permissionService, exportManager)
    {
        public void CheckTwoFactorEnabledWarningPublic(Customer existingCustomer, CustomerModel model) =>
            CheckTwoFactorEnabledWarning(existingCustomer, model);
    }

    private TestManagementController _controller;
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IRequestPermissionService> _permissionServiceMock;
    private Mock<IContextAccessor> _contextAccessorMock;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _permissionServiceMock = new Mock<IRequestPermissionService>();
        _contextAccessorMock = new Mock<IContextAccessor>();
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { Id = "admin-self" });
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var scopeMock = new Mock<IAdminDataScope<Customer>>();
        scopeMock.Setup(s => s.HasAccess(It.IsAny<Customer>())).ReturnsAsync(true);
        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new TestManagementController(_customerServiceMock.Object,
            new Mock<ICustomerViewModelService>().Object, new Mock<ICustomerManagerService>().Object,
            new Mock<ICustomerAttributeParser>().Object, new Mock<ICustomerAttributeService>().Object,
            new Mock<ICustomerMessageProviderService>().Object, _groupServiceMock.Object, translationServiceMock.Object,
            _contextAccessorMock.Object, new CustomerSettings(), scopeMock.Object,
            _permissionServiceMock.Object, new Mock<IExportManager<Customer>>().Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task Impersonate_NoPermission_ErrorsAndRedirectsToEdit_NoUserFieldUpdate()
    {
        var target = new Customer { Id = "c1" };
        _customerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(target);
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.AllowCustomerImpersonation)).ReturnsAsync(false);

        var result = await _controller.Impersonate("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _customerServiceMock.Verify(s => s.UpdateUserField(It.IsAny<Customer>(),
            SystemCustomerFieldNames.ImpersonatedCustomerId, It.IsAny<object>()), Times.Never);
    }

    [TestMethod]
    public async Task Impersonate_NonAdminImpersonatingAdmin_Denied()
    {
        var target = new Customer { Id = "c1" };
        _customerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(target);
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.AllowCustomerImpersonation)).ReturnsAsync(true);
        _groupServiceMock.Setup(g => g.IsAdmin(It.Is<Customer>(c => c.Id == "admin-self"))).ReturnsAsync(false);
        _groupServiceMock.Setup(g => g.IsAdmin(target)).ReturnsAsync(true);

        var result = await _controller.Impersonate("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _customerServiceMock.Verify(s => s.UpdateUserField(It.IsAny<Customer>(),
            SystemCustomerFieldNames.ImpersonatedCustomerId, It.IsAny<object>()), Times.Never);
    }

    [TestMethod]
    public void CheckTwoFactorEnabledWarning_CreateTime_NoExistingCustomer_WarnsIfEnabling()
    {
        var model = new CustomerModel { TwoFactorEnabled = true };

        _controller.CheckTwoFactorEnabledWarningPublic(null, model);

        Assert.IsTrue(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }

    [TestMethod]
    public void CheckTwoFactorEnabledWarning_EditTime_AlreadyEnabled_NoWarning()
    {
        var existing = new Customer();
        existing.UserFields.Add(new Grand.Domain.Common.UserField
            { Key = SystemCustomerFieldNames.TwoFactorEnabled, Value = "true", StoreId = "" });
        var model = new CustomerModel { TwoFactorEnabled = true };

        _controller.CheckTwoFactorEnabledWarningPublic(existing, model);

        Assert.IsFalse(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }
}
