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
using Grand.Web.AdminShared.Models.Common;
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
            translationService, contextAccessor, customerSettings, scope)
    {
        public Task<(Customer, IActionResult)> LoadAuthorizedCustomerPublic(string id) => LoadAuthorizedCustomer(id);
    }

    protected TestCustomerController Controller;
    protected Mock<ICustomerService> CustomerServiceMock;
    protected Mock<ICustomerViewModelService> CustomerViewModelServiceMock;
    protected Mock<ICustomerManagerService> CustomerManagerServiceMock;
    protected Mock<IGroupService> GroupServiceMock;
    protected Mock<IAdminDataScope<Customer>> ScopeMock;
    protected Mock<ITranslationService> TranslationServiceMock;
    protected Mock<IAddressAttributeService> AddressAttributeServiceMock;
    protected Mock<Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService> ProductReviewServiceMock;
    protected Mock<ICustomerProductService> CustomerProductServiceMock;

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
        AddressAttributeServiceMock = new Mock<IAddressAttributeService>();
        AddressAttributeServiceMock.Setup(a => a.GetAllAddressAttributes())
            .ReturnsAsync(new List<Grand.Domain.Common.AddressAttribute>());
        ProductReviewServiceMock = new Mock<Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService>();
        CustomerProductServiceMock = new Mock<ICustomerProductService>();

        var contextAccessorMock = new Mock<IContextAccessor>();

        Controller = new TestCustomerController(
            CustomerServiceMock.Object,
            CustomerViewModelServiceMock.Object,
            CustomerManagerServiceMock.Object,
            CustomerProductServiceMock.Object,
            ProductReviewServiceMock.Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IProductReviewViewModelService>().Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IProductViewModelService>().Object,
            new Mock<ICustomerAttributeParser>().Object,
            new Mock<ICustomerAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            AddressAttributeServiceMock.Object,
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

    [TestMethod]
    public async Task LoadAuthorizedCustomer_NotFound_ReturnsRedirectToList()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        var (customer, denied) = await Controller.LoadAuthorizedCustomerPublic("missing");

        Assert.IsNull(customer);
        var redirect = denied as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
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
        var contextAccessorMock = new Mock<IContextAccessor>();
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(self);
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        // Rebuild controller with a context accessor whose CurrentCustomer matches the target id.
        var controller = new TestCustomerController(CustomerServiceMock.Object, CustomerViewModelServiceMock.Object,
            CustomerManagerServiceMock.Object, new Mock<ICustomerProductService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService>().Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IProductReviewViewModelService>().Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IProductViewModelService>().Object,
            new Mock<ICustomerAttributeParser>().Object, new Mock<ICustomerAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object, new Mock<IAddressAttributeService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Messages.IMessageProviderService>().Object,
            GroupServiceMock.Object, TranslationServiceMock.Object, contextAccessorMock.Object,
            new CustomerSettings(), ScopeMock.Object)
        {
            ControllerContext = Controller.ControllerContext,
            TempData = Controller.TempData
        };
        CustomerServiceMock.Setup(s => s.GetCustomerById("self")).ReturnsAsync(self);
        ScopeMock.Setup(s => s.HasAccess(self)).ReturnsAsync(true);

        var result = await controller.Delete("self");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        CustomerViewModelServiceMock.Verify(v => v.DeleteCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task LoyaltyPointsHistoryAdd_StoreScope_ForcesStaffStoreId_IgnoringSubmittedStoreId()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerViewModelServiceMock
            .Setup(v => v.InsertLoyaltyPointsHistory(customer, "store-1", 10, "msg"))
            .ReturnsAsync(new Grand.Domain.Orders.LoyaltyPointsHistory());

        // Caller submits a different, attacker-controlled storeId — must be ignored under Store scope.
        await Controller.LoyaltyPointsHistoryAdd("c1", "attacker-store", 10, "msg");

        CustomerViewModelServiceMock.Verify(v => v.InsertLoyaltyPointsHistory(customer, "store-1", 10, "msg"), Times.Once);
    }

    [TestMethod]
    public async Task LoyaltyPointsHistoryAdd_GlobalScope_UsesSubmittedStoreId()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock
            .Setup(v => v.InsertLoyaltyPointsHistory(customer, "admin-submitted-store", 10, "msg"))
            .ReturnsAsync(new Grand.Domain.Orders.LoyaltyPointsHistory());

        await Controller.LoyaltyPointsHistoryAdd("c1", "admin-submitted-store", 10, "msg");

        CustomerViewModelServiceMock.Verify(v => v.InsertLoyaltyPointsHistory(customer, "admin-submitted-store", 10, "msg"), Times.Once);
    }

    [TestMethod]
    public async Task AddressesSelect_Denied_ThrowsArgumentException()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Controller.AddressesSelect("missing", new DataSourceRequest()));
    }

    [TestMethod]
    public async Task AddressCreatePost_Authorized_InsertsAndRedirects()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        var model = new CustomerAddressModel { CustomerId = "c1", Address = new AddressModel() };
        var inserted = new Grand.Domain.Common.Address { Id = "a1" };
        CustomerViewModelServiceMock
            .Setup(v => v.InsertAddressModel(customer, model, It.IsAny<List<Grand.Domain.Common.CustomAttribute>>()))
            .ReturnsAsync(inserted);

        var result = await Controller.AddressCreate(model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("AddressEdit", redirect.ActionName);
    }

    [TestMethod]
    public async Task OrderDetails_StoreScope_MismatchedStore_ReturnsEmptyNotException()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var orderServiceMock = new Mock<Grand.Business.Core.Interfaces.Checkout.Orders.IOrderService>();
        orderServiceMock.Setup(s => s.GetOrderById("o1"))
            .ReturnsAsync(new Grand.Domain.Orders.Order { Id = "o1", StoreId = "store-2" });

        var result = await Controller.OrderDetails("o1", orderServiceMock.Object,
            new Mock<Grand.Web.AdminShared.Interfaces.IOrderViewModelService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Common.Security.IPermissionService>().Object);

        var json = result as JsonResult;
        var data = json.Value as DataSourceResult;
        Assert.AreEqual(0, data.Total);
    }

    [TestMethod]
    public async Task OrderDetails_GlobalScope_NotFound_ThrowsArgumentException()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var permissionServiceMock = new Mock<Grand.Business.Core.Interfaces.Common.Security.IPermissionService>();
        permissionServiceMock.Setup(p => p.Authorize(Grand.Domain.Permissions.StandardPermission.ManageOrders)).ReturnsAsync(true);
        var orderServiceMock = new Mock<Grand.Business.Core.Interfaces.Checkout.Orders.IOrderService>();
        orderServiceMock.Setup(s => s.GetOrderById("missing")).ReturnsAsync((Grand.Domain.Orders.Order)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Controller.OrderDetails("missing", orderServiceMock.Object,
                new Mock<Grand.Web.AdminShared.Interfaces.IOrderViewModelService>().Object, permissionServiceMock.Object));
    }

    [TestMethod]
    public async Task GetCartList_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock.Setup(v => v.PrepareShoppingCartItemModel("any-customer-id", 1))
            .ReturnsAsync(new List<Grand.Web.AdminShared.Models.ShoppingCart.ShoppingCartItemModel>());

        var result = await Controller.GetCartList("any-customer-id", 1);

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetCartList_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.GetCartList("c1", 1);

        var json = result as JsonResult;
        var data = json.Value as DataSourceResult;
        Assert.AreEqual(0, data.Total);
        CustomerViewModelServiceMock.Verify(v => v.PrepareShoppingCartItemModel(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ReviewDelete_StoreScope_MismatchedStore_ThrowsArgumentException()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var review = new Grand.Domain.Catalog.ProductReview { Id = "r1", StoreId = "store-2" };
        ProductReviewServiceMock.Setup(s => s.GetProductReviewById("r1")).ReturnsAsync(review);

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.ReviewDelete("r1"));
    }

    [TestMethod]
    public async Task DeleteProductPrice_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock.Setup(v => v.DeleteProductPrice("pp1")).Returns(Task.CompletedTask);

        var result = await Controller.DeleteProductPrice("pp1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerViewModelServiceMock.Verify(v => v.DeleteProductPrice("pp1"), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductPrice_StoreScope_PriceNotFound_NoOpsSilently()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerProductServiceMock.Setup(s => s.GetCustomerProductPriceById("pp1"))
            .ReturnsAsync((Grand.Domain.Customers.CustomerProductPrice)null);

        var result = await Controller.DeleteProductPrice("pp1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerViewModelServiceMock.Verify(v => v.DeleteProductPrice(It.IsAny<string>()), Times.Never);
        CustomerProductServiceMock.Verify(s => s.DeleteCustomerProductPrice(It.IsAny<Grand.Domain.Customers.CustomerProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteProductPrice_StoreScope_DeniedOwnership_NoOpsSilently()
    {
        // Price exists but belongs to a customer outside the caller's store — exercises the
        // ownership-denial half of the guard, distinct from the not-found half above.
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var price = new Grand.Domain.Customers.CustomerProductPrice { Id = "pp1", CustomerId = "c1" };
        CustomerProductServiceMock.Setup(s => s.GetCustomerProductPriceById("pp1")).ReturnsAsync(price);
        var foreignCustomer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(foreignCustomer);
        ScopeMock.Setup(s => s.HasAccess(foreignCustomer)).ReturnsAsync(false);

        var result = await Controller.DeleteProductPrice("pp1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerViewModelServiceMock.Verify(v => v.DeleteProductPrice(It.IsAny<string>()), Times.Never);
        CustomerProductServiceMock.Verify(s => s.DeleteCustomerProductPrice(It.IsAny<Grand.Domain.Customers.CustomerProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAddPopup_Get_StoreScope_Denied_RedirectsToList()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.ProductAddPopup("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductsPrice_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.ProductsPrice(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var json = result as JsonResult;
        var data = json.Value as DataSourceResult;
        Assert.AreEqual(0, data.Total);
        CustomerViewModelServiceMock.Verify(
            v => v.PrepareProductPriceModel(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductsPrice_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock.Setup(v => v.PrepareProductPriceModel("c1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CustomerModel.ProductPriceModel>(), 0));

        var result = await Controller.ProductsPrice(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
        CustomerViewModelServiceMock.Verify(v => v.PrepareProductPriceModel("c1", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task PersonalizedProducts_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.PersonalizedProducts(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var json = result as JsonResult;
        var data = json.Value as DataSourceResult;
        Assert.AreEqual(0, data.Total);
        CustomerViewModelServiceMock.Verify(
            v => v.PreparePersonalizedProducts(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task PersonalizedProducts_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock.Setup(v => v.PreparePersonalizedProducts("c1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CustomerModel.ProductModel>(), 0));

        var result = await Controller.PersonalizedProducts(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
        CustomerViewModelServiceMock.Verify(v => v.PreparePersonalizedProducts("c1", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task ReviewList_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.ReviewList("c1", new DataSourceRequest { Page = 1, PageSize = 10 });

        var json = result as JsonResult;
        var data = json.Value as DataSourceResult;
        Assert.AreEqual(0, data.Total);
        ProductReviewServiceMock.Verify(s => s.GetAllProductReviews(
            It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ReviewList_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        ProductReviewServiceMock.Setup(s => s.GetAllProductReviews(
                "c1", null, null, null, "", null, "", 0, 10))
            .ReturnsAsync(new Grand.Domain.PagedList<Grand.Domain.Catalog.ProductReview>(
                new List<Grand.Domain.Catalog.ProductReview>(), 0, 10, 0));

        var result = await Controller.ReviewList("c1", new DataSourceRequest { Page = 1, PageSize = 10 });

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OutOfStockSubscriptionList_GlobalScope_NoOwnershipCheck()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        CustomerViewModelServiceMock
            .Setup(v => v.PrepareOutOfStockSubscriptionModel("c1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CustomerModel.OutOfStockSubscriptionModel>(), 0));

        var result = await Controller.OutOfStockSubscriptionList(
            new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
    }
}
