using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Common.RequestCustomerResolvers;
using Grand.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class ProductControllerReviewHelpfulnessTests
{
    private ProductController _controller;

    [TestInitialize]
    public void Init()
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "c1" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);

        _controller = new ProductController(
            new Mock<IProductService>().Object,
            contextAccessorMock.Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IRecentlyViewedProductsService>().Object,
            new Mock<IShoppingCartService>().Object,
            new Mock<IAclService>().Object,
            new Mock<IPermissionService>().Object,
            new Mock<IMediator>().Object,
            new CatalogSettings(),
            new CaptchaSettings(),
            new Mock<IGuestCustomerPersistence>().Object,
            new Mock<ILogger<ProductController>>().Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestMethod]
    public async Task SetProductReviewHelpfulness_ReviewDeletedMeanwhile_ReturnsNotFound()
    {
        var reviewService = new Mock<IProductReviewService>();
        reviewService.Setup(x => x.GetProductReviewById("gone")).ReturnsAsync((ProductReview)null);

        var result = await _controller.SetProductReviewHelpfulness("gone", "p1", true,
            new Mock<ICustomerService>().Object, new Mock<IGroupService>().Object, reviewService.Object);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }
}
