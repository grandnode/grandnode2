using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Mediator;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductReviewViewModelServiceTests
{
    private Mock<IMediator> _mediator;
    private Mock<IProductReviewService> _productReviewService;
    private Mock<IProductService> _productService;
    private ProductReviewViewModelService _service;

    public ProductReviewViewModelServiceTests()
    {
        _mediator = new Mock<IMediator>();
        _productReviewService = new Mock<IProductReviewService>();
        _productService = new Mock<IProductService>();
        _productService.Setup(x => x.GetProductById(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string id, bool _) => new Product { Id = id });

        _service = new ProductReviewViewModelService(_productService.Object, _productReviewService.Object,
            Mock.Of<ICustomerService>(), Mock.Of<IStoreService>(), Mock.Of<IDateTimeService>(),
            Mock.Of<IRequestTranslationService>(), _mediator.Object);
    }

    [TestMethod]
    public async Task ApproveSelected_UpdatesTotalsOfReviewsOwnProduct()
    {
        _productReviewService.Setup(x => x.GetProductReviewById("pr-1"))
            .ReturnsAsync(new ProductReview { Id = "pr-1", ProductId = "prod-1", StoreId = "store-1" });

        await _service.ApproveSelected(["pr-1:prod-other"], "store-1");

        _mediator.Verify(x => x.Send(It.Is<UpdateProductReviewTotalsCommand>(c => c.Product.Id == "prod-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _productService.Verify(x => x.GetProductById("prod-other", It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task DisapproveSelected_UpdatesTotalsOfReviewsOwnProduct()
    {
        _productReviewService.Setup(x => x.GetProductReviewById("pr-1"))
            .ReturnsAsync(new ProductReview { Id = "pr-1", ProductId = "prod-1", StoreId = "store-1", IsApproved = true });

        await _service.DisapproveSelected(["pr-1"], "store-1");

        _mediator.Verify(x => x.Send(It.Is<UpdateProductReviewTotalsCommand>(c => c.Product.Id == "prod-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
