using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Models;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

// Trimmed for the Task 11 thin-subclass cutover: business logic now shared through
// BaseCustomerController is exercised by BaseCustomerControllerTests.cs (Admin.Tests, Tasks 4-8)
// and applies identically to Store. What remains here is genuinely Store-only: the per-store
// feature gate (OnActionExecutionAsync), the PerStoreDisabled page, and the
// ApplyPostConstraints override's anti-smuggling contract.
[TestClass]
public class CustomerControllerTests
{
    private const string StoreId = "store-current";

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ICustomerViewModelService> _customerViewModelServiceMock;
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private Mock<ICustomerAttributeService> _customerAttributeServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<IAdminDataScope<Customer>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerViewModelServiceMock = new Mock<ICustomerViewModelService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _customerAttributeServiceMock = new Mock<ICustomerAttributeService>();
        _groupServiceMock = new Mock<IGroupService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StoreId });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _scopeMock = new Mock<IAdminDataScope<Customer>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns(StoreId);
    }

    /// <summary>Test-only subclass exposing the protected ApplyPostConstraints override for direct
    /// invocation, analogous to BaseCustomerControllerTests's LoadAuthorizedCustomerPublic
    /// wrapper.</summary>
    private class TestableCustomerController(
        ICustomerService customerService,
        ICustomerViewModelService customerViewModelService,
        ICustomerManagerService customerManagerService,
        ICustomerProductService customerProductService,
        IProductReviewService productReviewService,
        IProductReviewViewModelService productReviewViewModelService,
        IProductViewModelService productViewModelService,
        ICustomerAttributeParser customerAttributeParser,
        ICustomerAttributeService customerAttributeService,
        IAddressAttributeParser addressAttributeParser,
        IAddressAttributeService addressAttributeService,
        IMessageProviderService messageProviderService,
        IGroupService groupService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings,
        IAdminDataScope<Customer> scope,
        CustomerConfig customerConfig)
        : CustomerController(customerService, customerViewModelService, customerManagerService,
            customerProductService, productReviewService, productReviewViewModelService, productViewModelService,
            customerAttributeParser, customerAttributeService, addressAttributeParser, addressAttributeService,
            messageProviderService, groupService, translationService, contextAccessor, customerSettings, scope,
            customerConfig)
    {
        public Task ApplyPostConstraintsPublic(CustomerModel model) => ApplyPostConstraints(model);
    }

    private TestableCustomerController BuildStoreController(bool perStoreEnabled = true,
        IGroupService groupService = null, IAdminDataScope<Customer> scope = null)
    {
        var controller = new TestableCustomerController(
            _customerServiceMock.Object,
            _customerViewModelServiceMock.Object,
            _customerManagerServiceMock.Object,
            new Mock<ICustomerProductService>().Object,
            new Mock<IProductReviewService>().Object,
            new Mock<IProductReviewViewModelService>().Object,
            new Mock<IProductViewModelService>().Object,
            new Mock<ICustomerAttributeParser>().Object,
            _customerAttributeServiceMock.Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IMessageProviderService>().Object,
            groupService ?? _groupServiceMock.Object,
            _translationServiceMock.Object,
            _contextAccessorMock.Object,
            new CustomerSettings(),
            scope ?? _scopeMock.Object,
            new CustomerConfig { RegisterCustomersPerStore = perStoreEnabled });

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        return controller;
    }

    private static ActionExecutingContext BuildGate(CustomerController controller, string actionName)
    {
        var actionContext = new ActionContext(controller.ControllerContext.HttpContext, new RouteData(),
            new ControllerActionDescriptor { ActionName = actionName });
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object>(), controller);
    }

    [TestMethod]
    public async Task Gate_PerStoreDisabled_RedirectsToPerStoreDisabled()
    {
        var controller = BuildStoreController(perStoreEnabled: false);
        var context = BuildGate(controller, nameof(CustomerController.List));
        var nextCalled = false;

        await controller.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), controller));
        });

        Assert.IsFalse(nextCalled);
        var redirect = context.Result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual(nameof(CustomerController.PerStoreDisabled), redirect.ActionName);
    }

    [TestMethod]
    public async Task Gate_PerStoreDisabled_AllowsPerStoreDisabledAction()
    {
        var controller = BuildStoreController(perStoreEnabled: false);
        var context = BuildGate(controller, nameof(CustomerController.PerStoreDisabled));
        var nextCalled = false;

        await controller.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), controller));
        });

        Assert.IsTrue(nextCalled);
        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public async Task Gate_PerStoreEnabled_CallsNext()
    {
        var controller = BuildStoreController(perStoreEnabled: true);
        var context = BuildGate(controller, nameof(CustomerController.List));
        var nextCalled = false;

        await controller.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), controller));
        });

        Assert.IsTrue(nextCalled);
        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public void PerStoreDisabled_ReturnsView()
    {
        var controller = BuildStoreController(perStoreEnabled: false);
        var result = controller.PerStoreDisabled();
        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task ApplyPostConstraints_CraftedPost_CannotSmuggleOwnershipFields()
    {
        // Anti-smuggling proof required by the design spec: a caller-crafted CustomerModel claiming
        // a foreign StoreId/Owner/VendorId/StaffStoreId/SeId/CustomerGroups must be fully overwritten,
        // not merely "some field got touched."
        var groupServiceMock = new Mock<IGroupService>();
        var registered = new CustomerGroup { Id = "registered-id" };
        groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync(registered);
        var scopeMock = new Mock<IAdminDataScope<Customer>>();
        scopeMock.Setup(s => s.DefaultStoreId).Returns("legit-store-1");
        var controller = BuildStoreController(groupService: groupServiceMock.Object, scope: scopeMock.Object);

        var craftedModel = new CustomerModel {
            StoreId = "attacker-foreign-store",
            Owner = "attacker-owner-id",
            VendorId = "attacker-vendor-id",
            StaffStoreId = "attacker-staff-store-id",
            SeId = "attacker-se-id",
            CustomerGroups = new[] { "attacker-admin-group-id" }
        };

        await controller.ApplyPostConstraintsPublic(craftedModel);

        // Assert the ACTUAL enforced values, not merely that the method ran (the Phase 19
        // MessageTemplate review lesson this task explicitly carries forward).
        Assert.AreEqual("legit-store-1", craftedModel.StoreId);
        Assert.AreEqual("", craftedModel.Owner);
        Assert.AreEqual("", craftedModel.VendorId);
        Assert.AreEqual("", craftedModel.StaffStoreId);
        Assert.AreEqual("", craftedModel.SeId);
        CollectionAssert.AreEqual(new[] { "registered-id" }, craftedModel.CustomerGroups.ToArray());
    }

    [TestMethod]
    public async Task ApplyPostConstraints_NoRegisteredGroupFound_ClearsCustomerGroups()
    {
        // Edge case the crafted-POST test doesn't cover: if the Registered group is missing/
        // misconfigured, the override must not fall back to whatever the caller submitted — it
        // must end up empty, never the attacker-supplied group list.
        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync((CustomerGroup)null);
        var scopeMock = new Mock<IAdminDataScope<Customer>>();
        scopeMock.Setup(s => s.DefaultStoreId).Returns("legit-store-1");
        var controller = BuildStoreController(groupService: groupServiceMock.Object, scope: scopeMock.Object);

        var craftedModel = new CustomerModel { CustomerGroups = new[] { "attacker-admin-group-id" } };

        await controller.ApplyPostConstraintsPublic(craftedModel);

        Assert.AreEqual(0, craftedModel.CustomerGroups.Count());
    }
}
