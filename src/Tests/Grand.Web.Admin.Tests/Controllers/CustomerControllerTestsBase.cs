using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
///     Fixture shared by the tests of BaseCustomerController and the per-tab customer controllers split out
///     of it. Each derived class only says how to build its controller from the mocks below.
/// </summary>
public abstract class CustomerControllerTestsBase<TController> where TController : Controller
{
    protected TController Controller;
    protected Mock<ICustomerService> CustomerServiceMock;
    protected Mock<ICustomerViewModelService> CustomerViewModelServiceMock;
    protected Mock<ICustomerAddressViewModelService> CustomerAddressViewModelServiceMock;
    protected Mock<ICustomerLoyaltyPointsViewModelService> CustomerLoyaltyPointsViewModelServiceMock;
    protected Mock<ICustomerShoppingCartViewModelService> CustomerShoppingCartViewModelServiceMock;
    protected Mock<ICustomerProductViewModelService> CustomerProductViewModelServiceMock;
    protected Mock<ICustomerOutOfStockSubscriptionViewModelService> CustomerOutOfStockSubscriptionViewModelServiceMock;
    protected Mock<ICustomerNoteViewModelService> CustomerNoteViewModelServiceMock;
    protected Mock<ICustomerManagerService> CustomerManagerServiceMock;
    protected Mock<ICustomerProductService> CustomerProductServiceMock;
    protected Mock<IProductReviewService> ProductReviewServiceMock;
    protected Mock<IProductReviewViewModelService> ProductReviewViewModelServiceMock;
    protected Mock<IProductViewModelService> ProductViewModelServiceMock;
    protected Mock<IOrderService> OrderServiceMock;
    protected Mock<IOrderViewModelService> OrderViewModelServiceMock;
    protected Mock<IRequestPermissionService> PermissionServiceMock;
    protected Mock<ICustomerAttributeParser> CustomerAttributeParserMock;
    protected Mock<ICustomerAttributeService> CustomerAttributeServiceMock;
    protected Mock<IAddressAttributeParser> AddressAttributeParserMock;
    protected Mock<IAddressAttributeService> AddressAttributeServiceMock;
    protected Mock<IMessageProviderService> MessageProviderServiceMock;
    protected Mock<IGroupService> GroupServiceMock;
    protected Mock<IRequestTranslationService> TranslationServiceMock;
    protected Mock<IContextAccessor> ContextAccessorMock;
    protected Mock<IAdminDataScope<Customer>> ScopeMock;

    protected abstract TController CreateController();

    [TestInitialize]
    public void Setup()
    {
        CustomerServiceMock = new Mock<ICustomerService>();
        CustomerViewModelServiceMock = new Mock<ICustomerViewModelService>();
        CustomerAddressViewModelServiceMock = new Mock<ICustomerAddressViewModelService>();
        CustomerLoyaltyPointsViewModelServiceMock = new Mock<ICustomerLoyaltyPointsViewModelService>();
        CustomerShoppingCartViewModelServiceMock = new Mock<ICustomerShoppingCartViewModelService>();
        CustomerProductViewModelServiceMock = new Mock<ICustomerProductViewModelService>();
        CustomerOutOfStockSubscriptionViewModelServiceMock = new Mock<ICustomerOutOfStockSubscriptionViewModelService>();
        CustomerNoteViewModelServiceMock = new Mock<ICustomerNoteViewModelService>();
        CustomerManagerServiceMock = new Mock<ICustomerManagerService>();
        CustomerProductServiceMock = new Mock<ICustomerProductService>();
        ProductReviewServiceMock = new Mock<IProductReviewService>();
        ProductReviewViewModelServiceMock = new Mock<IProductReviewViewModelService>();
        ProductViewModelServiceMock = new Mock<IProductViewModelService>();
        OrderServiceMock = new Mock<IOrderService>();
        OrderViewModelServiceMock = new Mock<IOrderViewModelService>();
        PermissionServiceMock = new Mock<IRequestPermissionService>();
        CustomerAttributeParserMock = new Mock<ICustomerAttributeParser>();
        CustomerAttributeServiceMock = new Mock<ICustomerAttributeService>();
        AddressAttributeParserMock = new Mock<IAddressAttributeParser>();
        AddressAttributeServiceMock = new Mock<IAddressAttributeService>();
        AddressAttributeServiceMock.Setup(a => a.GetAllAddressAttributes())
            .ReturnsAsync(new List<Grand.Domain.Common.AddressAttribute>());
        MessageProviderServiceMock = new Mock<IMessageProviderService>();
        GroupServiceMock = new Mock<IGroupService>();
        TranslationServiceMock = new Mock<IRequestTranslationService>();
        TranslationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        ContextAccessorMock = new Mock<IContextAccessor>();
        ScopeMock = new Mock<IAdminDataScope<Customer>>();
        ScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        Controller = CreateController();

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

    /// <summary>Makes customerId resolve to a customer the scope grants access to.</summary>
    protected Customer SetupAuthorizedCustomer(string customerId)
    {
        var customer = new Customer { Id = customerId };
        CustomerServiceMock.Setup(s => s.GetCustomerById(customerId)).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(true);
        return customer;
    }
}
