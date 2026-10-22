using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerControllerTests : CustomerControllerTestsBase<BaseCustomerControllerTests.TestCustomerController>
{
    // BaseCustomerController is abstract; minimal subclass so actions can be invoked directly.
    public class TestCustomerController(
        Grand.Business.Core.Interfaces.Customers.ICustomerService customerService,
        Grand.Web.AdminShared.Interfaces.ICustomerViewModelService customerViewModelService,
        Grand.Business.Core.Interfaces.Customers.ICustomerManagerService customerManagerService,
        Grand.Business.Core.Interfaces.Customers.ICustomerAttributeParser customerAttributeParser,
        Grand.Business.Core.Interfaces.Customers.ICustomerAttributeService customerAttributeService,
        Grand.Business.Core.Interfaces.Messages.IMessageProviderService messageProviderService,
        Grand.Business.Core.Interfaces.Common.Directory.IGroupService groupService,
        Grand.Web.Common.Localization.IRequestTranslationService translationService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings,
        Grand.Web.AdminShared.Interfaces.IAdminDataScope<Customer> scope)
        : BaseCustomerController(customerService, customerViewModelService, customerManagerService,
            customerAttributeParser, customerAttributeService, messageProviderService, groupService,
            translationService, contextAccessor, customerSettings, scope)
    {
        public Task<(Customer, IActionResult)> LoadAuthorizedCustomerPublic(string id) => LoadAuthorizedCustomer(id);
    }

    protected override TestCustomerController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerViewModelServiceMock.Object,
        CustomerManagerServiceMock.Object,
        CustomerAttributeParserMock.Object,
        CustomerAttributeServiceMock.Object,
        MessageProviderServiceMock.Object,
        GroupServiceMock.Object,
        TranslationServiceMock.Object,
        ContextAccessorMock.Object,
        new CustomerSettings(),
        ScopeMock.Object);


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
    public async Task CreateGet_StoreScope_ClearsAvailableStores()
    {
        // Regression test for the cross-store leak flagged in code review: PrepareCustomerModel
        // always populates every store's id/name onto AvailableStores (used for the
        // StaffStoreId/StoreId pickers), but Store's own view never renders either select and the
        // model still reaches Store's store_customer_details_* widget zones via
        // additional-data="Model" - other stores' names/ids must not leak there.
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareCustomerModel(It.IsAny<CustomerModel>(), null, false))
            .Callback<CustomerModel, Customer, bool>((m, _, _) =>
                m.AvailableStores.Add(new SelectListItem { Value = "store-1", Text = "Store 1" }))
            .Returns(Task.CompletedTask);

        var result = await Controller.Create();

        var model = (CustomerModel)((ViewResult)result).Model;
        Assert.AreEqual(0, model.AvailableStores.Count);
    }

    [TestMethod]
    public async Task CreateGet_GlobalScope_KeepsAvailableStores()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareCustomerModel(It.IsAny<CustomerModel>(), null, false))
            .Callback<CustomerModel, Customer, bool>((m, _, _) =>
                m.AvailableStores.Add(new SelectListItem { Value = "store-1", Text = "Store 1" }))
            .Returns(Task.CompletedTask);

        var result = await Controller.Create();

        var model = (CustomerModel)((ViewResult)result).Model;
        Assert.AreEqual(1, model.AvailableStores.Count);
    }

    [TestMethod]
    public async Task EditGet_StoreScope_ClearsAvailableStores()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareCustomerModel(It.IsAny<CustomerModel>(), customer, false))
            .Callback<CustomerModel, Customer, bool>((m, _, _) =>
                m.AvailableStores.Add(new SelectListItem { Value = "store-1", Text = "Store 1" }))
            .Returns(Task.CompletedTask);

        var result = await Controller.Edit("c1");

        var model = (CustomerModel)((ViewResult)result).Model;
        Assert.AreEqual(0, model.AvailableStores.Count);
    }

    [TestMethod]
    public async Task EditGet_GlobalScope_KeepsAvailableStores()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareCustomerModel(It.IsAny<CustomerModel>(), customer, false))
            .Callback<CustomerModel, Customer, bool>((m, _, _) =>
                m.AvailableStores.Add(new SelectListItem { Value = "store-1", Text = "Store 1" }))
            .Returns(Task.CompletedTask);

        var result = await Controller.Edit("c1");

        var model = (CustomerModel)((ViewResult)result).Model;
        Assert.AreEqual(1, model.AvailableStores.Count);
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

    [TestMethod]
    public async Task LoadAuthorizedCustomer_NotFound_ReturnsRedirectToList()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        var (customer, denied) = await Controller.LoadAuthorizedCustomerPublic("missing");

        Assert.IsNull(customer);
        var redirect = denied as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Customer", redirect.ControllerName);
        ScopeMock.Verify(s => s.HasAccess(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadAuthorizedCustomer_ScopeDenies_ReturnsRedirectToList()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(false);

        var (result, denied) = await Controller.LoadAuthorizedCustomerPublic("c1");

        Assert.IsNull(result);
        var redirect = denied as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task LoadAuthorizedCustomer_ScopeAllows_ReturnsCustomerNoDenial()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);

        var (result, denied) = await Controller.LoadAuthorizedCustomerPublic("c1");

        Assert.AreSame(customer, result);
        Assert.IsNull(denied);
    }

    [TestMethod]
    public async Task EditGet_Authorized_ReturnsViewAndCallsPrepareCustomerModel()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);

        var result = await Controller.Edit("c1");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        CustomerViewModelServiceMock.Verify(v => v.PrepareCustomerModel(It.IsAny<CustomerModel>(), customer, false), Times.Once);
    }

    [TestMethod]
    public async Task EditGet_Denied_RedirectsToList()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        var result = await Controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Delete_SelfDelete_ErrorsAndRedirectsWithoutDeleting()
    {
        var self = new Customer { Id = "self" };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(self);
        ContextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        CustomerServiceMock.Setup(s => s.GetCustomerById("self")).ReturnsAsync(self);
        ScopeMock.Setup(s => s.HasAccess(self)).ReturnsAsync(true);

        var result = await Controller.Delete("self");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        CustomerViewModelServiceMock.Verify(v => v.DeleteCustomer(It.IsAny<Customer>()), Times.Never);
    }


    [TestMethod]
    public async Task ExternalAuthenticationDelete_CustomerInScope_RemovesTheLink()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        var links = new Mock<Grand.Business.Core.Interfaces.Authentication.IOidcLinkService>();
        links.Setup(l => l.Remove(customer, "link-1")).ReturnsAsync(true);

        var result = await Controller.ExternalAuthenticationDelete("link-1", "c1", links.Object);

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        links.Verify(l => l.Remove(customer, "link-1"), Times.Once);
    }

    [TestMethod]
    public async Task ExternalAuthenticationDelete_CustomerOutOfScope_Throws_NothingRemoved()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(false);
        var links = new Mock<Grand.Business.Core.Interfaces.Authentication.IOidcLinkService>();

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.ExternalAuthenticationDelete("link-1", "c1", links.Object));
        links.Verify(l => l.Remove(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }
}
