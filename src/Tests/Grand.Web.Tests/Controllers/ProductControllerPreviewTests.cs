using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     The admin "Preview" button opens the storefront page of the edited product. A product whose availability
///     window has not started (or has already ended) must stay hidden from customers, but a catalog manager has to
///     be able to preview it - exactly like an unpublished one.
/// </summary>
[TestClass]
public class ProductControllerPreviewTests
{
    private Mock<IAclService> _aclServiceMock;
    private ProductController _controller;
    private Customer _customer;
    private Mock<IMediator> _mediatorMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private Mock<IProductService> _productServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "c1" };
        var store = new Grand.Domain.Stores.Store { Id = "s1" };
        var language = new Language { Id = "l1" };

        _productServiceMock = new Mock<IProductService>();
        _aclServiceMock = new Mock<IAclService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _mediatorMock = new Mock<IMediator>();

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.CurrentCustomer).Returns(_customer);
        workContextMock.Setup(x => x.WorkingLanguage).Returns(language);
        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(x => x.CurrentStore).Returns(store);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);
        contextAccessorMock.Setup(x => x.StoreContext).Returns(storeContextMock.Object);

        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Product>(), It.IsAny<Customer>())).Returns(true);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Product>(), It.IsAny<string>())).Returns(true);

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetProductDetailsPage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDetailsModel());
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetProductLayoutViewPath>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ProductLayout.Simple");

        _controller = new ProductController(
            _productServiceMock.Object,
            contextAccessorMock.Object,
            new Mock<ITranslationService>().Object,
            new Mock<IRecentlyViewedProductsService>().Object,
            new Mock<IShoppingCartService>().Object,
            _aclServiceMock.Object,
            _permissionServiceMock.Object,
            _mediatorMock.Object,
            new CatalogSettings { AllowViewUnpublishedProductPage = false },
            new CaptchaSettings()) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { Request = { Method = "GET" } }
            }
        };
    }

    private void GivenProduct(Product product)
    {
        _productServiceMock.Setup(x => x.GetProductById(product.Id, false)).ReturnsAsync(product);
    }

    private static Product NotYetAvailableProduct()
    {
        return new Product {
            Id = "p1",
            Published = true,
            VisibleIndividually = true,
            AvailableStartDateTimeUtc = DateTime.UtcNow.AddDays(30)
        };
    }

    [TestMethod]
    public async Task ProductDetails_NotYetAvailable_AnonymousCustomerGetsNotFound()
    {
        GivenProduct(NotYetAvailableProduct());
        _permissionServiceMock
            .Setup(x => x.Authorize(StandardPermission.ManageProducts, _customer))
            .ReturnsAsync(false);

        var result = await _controller.ProductDetails("p1");

        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task ProductDetails_NotYetAvailable_CatalogManagerCanPreview()
    {
        GivenProduct(NotYetAvailableProduct());
        _permissionServiceMock
            .Setup(x => x.Authorize(StandardPermission.ManageProducts, _customer))
            .ReturnsAsync(true);

        var result = await _controller.ProductDetails("p1");

        Assert.IsNotInstanceOfType<NotFoundResult>(result.Result);
        Assert.IsInstanceOfType<ViewResult>(result.Result);
    }

    [TestMethod]
    public async Task ProductDetails_AvailabilityEnded_CatalogManagerCanPreview()
    {
        var product = NotYetAvailableProduct();
        product.AvailableStartDateTimeUtc = null;
        product.AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(-1);
        GivenProduct(product);
        _permissionServiceMock
            .Setup(x => x.Authorize(StandardPermission.ManageProducts, _customer))
            .ReturnsAsync(true);

        var result = await _controller.ProductDetails("p1");

        Assert.IsInstanceOfType<ViewResult>(result.Result);
    }
}
