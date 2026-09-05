using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerControllerTests
{
    // BaseCustomerController is abstract; minimal subclass so actions can be invoked directly.
    // The full 17-dependency constructor is declared once here; every later task's tests reuse
    // this same TestCustomerController and Setup().
    protected class TestCustomerController(
        ICustomerService customerService,
        ICustomerViewModelService customerViewModelService,
        ICustomerManagerService customerManagerService,
        ICustomerProductService customerProductService,
        Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService productReviewService,
        Grand.Web.AdminShared.Interfaces.IProductReviewViewModelService productReviewViewModelService,
        Grand.Web.AdminShared.Interfaces.IProductViewModelService productViewModelService,
        ICustomerAttributeParser customerAttributeParser,
        ICustomerAttributeService customerAttributeService,
        IAddressAttributeParser addressAttributeParser,
        IAddressAttributeService addressAttributeService,
        Grand.Business.Core.Interfaces.Messages.IMessageProviderService messageProviderService,
        IGroupService groupService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings,
        IAdminDataScope<Customer> scope)
        : BaseCustomerController(customerService, customerViewModelService, customerManagerService,
            customerProductService, productReviewService, productReviewViewModelService,
            productViewModelService, customerAttributeParser, customerAttributeService,
            addressAttributeParser, addressAttributeService, messageProviderService, groupService,
            translationService, contextAccessor, customerSettings, scope);

    protected TestCustomerController Controller;
    protected Mock<ICustomerService> CustomerServiceMock;
    protected Mock<ICustomerViewModelService> CustomerViewModelServiceMock;
    protected Mock<ICustomerManagerService> CustomerManagerServiceMock;
    protected Mock<IGroupService> GroupServiceMock;
    protected Mock<IAdminDataScope<Customer>> ScopeMock;
    protected Mock<ITranslationService> TranslationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        CustomerServiceMock = new Mock<ICustomerService>();
        CustomerViewModelServiceMock = new Mock<ICustomerViewModelService>();
        CustomerManagerServiceMock = new Mock<ICustomerManagerService>();
        GroupServiceMock = new Mock<IGroupService>();
        ScopeMock = new Mock<IAdminDataScope<Customer>>();
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        TranslationServiceMock = new Mock<ITranslationService>();
        TranslationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var contextAccessorMock = new Mock<IContextAccessor>();

        Controller = new TestCustomerController(
            CustomerServiceMock.Object,
            CustomerViewModelServiceMock.Object,
            CustomerManagerServiceMock.Object,
            new Mock<ICustomerProductService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService>().Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IProductReviewViewModelService>().Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IProductViewModelService>().Object,
            new Mock<ICustomerAttributeParser>().Object,
            new Mock<ICustomerAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Messages.IMessageProviderService>().Object,
            GroupServiceMock.Object,
            TranslationServiceMock.Object,
            contextAccessorMock.Object,
            new CustomerSettings(),
            ScopeMock.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        Controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task List_CallsPrepareCustomerListModel_ForBothHosts()
    {
        // The fix: both Admin and Store must call the shared, fully-populated
        // PrepareCustomerListModel() — Store's original bypassed it entirely (`new CustomerListModel()`),
        // leaving CustomerSettings-driven column-visibility flags always false regardless of config.
        CustomerViewModelServiceMock.Setup(v => v.PrepareCustomerListModel())
            .ReturnsAsync(new CustomerListModel { UsernamesEnabled = true, CompanyEnabled = true });

        var result = await Controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as CustomerListModel;
        Assert.IsTrue(model.UsernamesEnabled);
        Assert.IsTrue(model.CompanyEnabled);
        CustomerViewModelServiceMock.Verify(v => v.PrepareCustomerListModel(), Times.Once);
    }

    [TestMethod]
    public async Task CustomerList_GlobalScope_UsesSubmittedGroupIds()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareCustomerList(It.IsAny<CustomerListModel>(),
                new[] { "group-1" }, null, 1, 10, ""))
            .ReturnsAsync((Enumerable.Empty<CustomerModel>(), 0));

        await Controller.CustomerList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new CustomerListModel(), new[] { "group-1" }, null);

        CustomerViewModelServiceMock.Verify(v => v.PrepareCustomerList(It.IsAny<CustomerListModel>(),
            new[] { "group-1" }, null, 1, 10, ""), Times.Once);
    }

    [TestMethod]
    public async Task CustomerList_StoreScope_ForcesRegisteredGroupAndStoreId()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var registered = new CustomerGroup { Id = "registered-id" };
        GroupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync(registered);
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareCustomerList(It.IsAny<CustomerListModel>(),
                new[] { "registered-id" }, null, 1, 10, "store-1"))
            .ReturnsAsync((Enumerable.Empty<CustomerModel>(), 0));

        // Attacker/UI submits an arbitrary group filter — must be ignored under Store scope.
        await Controller.CustomerList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new CustomerListModel(), new[] { "attacker-submitted-group" }, null);

        CustomerViewModelServiceMock.Verify(v => v.PrepareCustomerList(It.IsAny<CustomerListModel>(),
            new[] { "registered-id" }, null, 1, 10, "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task CreateGet_GlobalScope_DoesNotCallApplyPostConstraints_ModelUntouched()
    {
        CustomerViewModelServiceMock.Setup(v => v.PrepareCustomerModel(It.IsAny<CustomerModel>(), null, false))
            .Returns(Task.CompletedTask);

        var result = await Controller.Create();

        var view = result as ViewResult;
        var model = view.Model as CustomerModel;
        Assert.IsTrue(model.Active);
        Assert.AreEqual("", model.StoreId ?? ""); // never forced for Admin (default no-op hook)
    }

    [TestMethod]
    public async Task CreatePost_Invalid_TwoFactorWarningHookNotCalledOnBase()
    {
        // BaseCustomerController's own CheckTwoFactorEnabledWarning is a no-op — verified indirectly:
        // no exception, no TempData warning entry set, for a base-level (non-Management) controller.
        var model = new CustomerModel { TwoFactorEnabled = true };
        Controller.ModelState.AddModelError("x", "invalid");
        CustomerViewModelServiceMock.Setup(v => v.PrepareCustomerModel(model, null, true))
            .Returns(Task.CompletedTask);

        var result = await Controller.Create(model, false);

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(Controller.TempData.ContainsKey("grand.notifications.Warning"));
    }
}
