using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerReviewControllerTests
    : CustomerControllerTestsBase<BaseCustomerReviewControllerTests.TestCustomerReviewController>
{
    public class TestCustomerReviewController(
        ICustomerService customerService,
        IProductReviewService productReviewService,
        IProductReviewViewModelService productReviewViewModelService,
        IProductViewModelService productViewModelService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerReviewController(customerService, productReviewService, productReviewViewModelService,
            productViewModelService, scope);

    protected override TestCustomerReviewController CreateController() => new(
        CustomerServiceMock.Object,
        ProductReviewServiceMock.Object,
        ProductReviewViewModelServiceMock.Object,
        ProductViewModelServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task ReviewDelete_StoreScope_MismatchedStore_ThrowsArgumentException()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        ProductReviewServiceMock.Setup(s => s.GetProductReviewById("r1"))
            .ReturnsAsync(new ProductReview { Id = "r1", StoreId = "store-2" });

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.ReviewDelete("r1"));
        ProductReviewViewModelServiceMock.Verify(v => v.DeleteProductReview(It.IsAny<ProductReview>()), Times.Never);
    }

    [TestMethod]
    public async Task ReviewList_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.ReviewList("c1", new DataSourceRequest { Page = 1, PageSize = 10 });

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        ProductReviewServiceMock.Verify(s => s.GetAllProductReviews(
            It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ReviewList_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        ProductReviewServiceMock.Setup(s => s.GetAllProductReviews("c1", null, null, null, "", null, "", 0, 10))
            .ReturnsAsync(new PagedList<ProductReview>(new List<ProductReview>(), 0, 10, 0));

        var result = await Controller.ReviewList("c1", new DataSourceRequest { Page = 1, PageSize = 10 });

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
    }
}
