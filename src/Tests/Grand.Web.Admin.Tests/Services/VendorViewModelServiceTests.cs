using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Vendors;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class VendorViewModelServiceTests
{
    private Mock<IDiscountService> _discountServiceMock;
    private Mock<IVendorService> _vendorServiceMock;
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IDateTimeService> _dateTimeServiceMock;
    private Mock<ICountryService> _countryServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IPictureService> _pictureServiceMock;
    private Mock<IMediator> _mediatorMock;
    private VendorViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _discountServiceMock = new Mock<IDiscountService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _countryServiceMock = new Mock<ICountryService>();
        _storeServiceMock = new Mock<IStoreService>();
        _pictureServiceMock = new Mock<IPictureService>();
        _mediatorMock = new Mock<IMediator>();

        _service = new VendorViewModelService(
            _discountServiceMock.Object,
            _vendorServiceMock.Object,
            _customerServiceMock.Object,
            _translationServiceMock.Object,
            _dateTimeServiceMock.Object,
            _countryServiceMock.Object,
            _storeServiceMock.Object,
            _pictureServiceMock.Object,
            _mediatorMock.Object,
            new VendorSettings(),
            new Mock<ISeNameService>().Object);
    }

    [TestMethod]
    public async Task PrepareVendorReviewModel_Entity_AlwaysPopulatesVendorIdAndName()
    {
        var vendorReview = new VendorReview { Id = "review-1", VendorId = "vendor-1", CustomerId = "cust-1" };
        _vendorServiceMock.Setup(v => v.GetVendorById("vendor-1"))
            .ReturnsAsync(new Vendor { Id = "vendor-1", Name = "Acme Vendor" });
        _customerServiceMock.Setup(c => c.GetCustomerById("cust-1"))
            .ReturnsAsync(new Customer { Id = "cust-1", Email = "buyer@example.com" });

        var model = new VendorReviewModel();
        await _service.PrepareVendorReviewModel(model, vendorReview, false, false);

        Assert.AreEqual("vendor-1", model.VendorId);
        Assert.AreEqual("Acme Vendor", model.VendorName);
        Assert.AreEqual("review-1:vendor-1", model.Ids);
    }

    [TestMethod]
    public async Task PrepareVendorReviewModel_List_GlobalScope_UsesModelSearchVendorId()
    {
        _vendorServiceMock.Setup(v => v.GetAllVendorReviews("", null, null, null, null, "vendor-picked", 0, 10))
            .ReturnsAsync(new PagedList<VendorReview>(new List<VendorReview>(), 0, 10));

        var listModel = new VendorReviewListModel { SearchVendorId = "vendor-picked" };
        await _service.PrepareVendorReviewModel(listModel, 1, 10);

        _vendorServiceMock.Verify(v => v.GetAllVendorReviews("", null, null, null, null, "vendor-picked", 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task ApproveVendorReviews_GlobalScope_ApprovesRegardlessOfVendor()
    {
        var review = new VendorReview { Id = "review-1", VendorId = "vendor-1", IsApproved = false };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("review-1")).ReturnsAsync(review);
        _vendorServiceMock.Setup(v => v.GetVendorById("vendor-1")).ReturnsAsync(new Vendor { Id = "vendor-1" });
        var globalScope = new GlobalAdminDataScope<VendorReview>();

        await _service.ApproveVendorReviews(new[] { "review-1:vendor-1" }, globalScope);

        Assert.IsTrue(review.IsApproved);
        _vendorServiceMock.Verify(v => v.UpdateVendorReview(review), Times.Once);
    }

    [TestMethod]
    public async Task ApproveVendorReviews_VendorScope_SkipsOtherVendorsReview()
    {
        var review = new VendorReview { Id = "review-1", VendorId = "vendor-OTHER", IsApproved = false };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("review-1")).ReturnsAsync(review);
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = "vendor-mine" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        var vendorScope = new VendorVendorReviewDataScope(contextAccessorMock.Object);

        await _service.ApproveVendorReviews(new[] { "review-1:vendor-OTHER" }, vendorScope);

        Assert.IsFalse(review.IsApproved);
        _vendorServiceMock.Verify(v => v.UpdateVendorReview(It.IsAny<VendorReview>()), Times.Never);
    }

    [TestMethod]
    public async Task DisapproveVendorReviews_VendorScope_SkipsOtherVendorsReview()
    {
        var review = new VendorReview { Id = "review-1", VendorId = "vendor-OTHER", IsApproved = true };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("review-1")).ReturnsAsync(review);
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = "vendor-mine" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        var vendorScope = new VendorVendorReviewDataScope(contextAccessorMock.Object);

        await _service.DisapproveVendorReviews(new[] { "review-1:vendor-OTHER" }, vendorScope);

        Assert.IsTrue(review.IsApproved);
        _vendorServiceMock.Verify(v => v.UpdateVendorReview(It.IsAny<VendorReview>()), Times.Never);
    }

    [TestMethod]
    public async Task ApproveVendorReviews_MismatchedCompositeVendorId_UsesEntityVendorIdNotClientSuppliedId()
    {
        var review = new VendorReview { Id = "review-1", VendorId = "vendor-1", IsApproved = false };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("review-1")).ReturnsAsync(review);
        _vendorServiceMock.Setup(v => v.GetVendorById(It.IsAny<string>())).ReturnsAsync(new Vendor { Id = "vendor-1" });
        var globalScope = new GlobalAdminDataScope<VendorReview>();

        await _service.ApproveVendorReviews(new[] { "review-1:vendor-WRONG" }, globalScope);

        _vendorServiceMock.Verify(v => v.GetVendorById("vendor-1"), Times.Once);
        _vendorServiceMock.Verify(v => v.GetVendorById("vendor-WRONG"), Times.Never);
    }

    [TestMethod]
    public async Task DisapproveVendorReviews_MismatchedCompositeVendorId_UsesEntityVendorIdNotClientSuppliedId()
    {
        var review = new VendorReview { Id = "review-1", VendorId = "vendor-1", IsApproved = true };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("review-1")).ReturnsAsync(review);
        _vendorServiceMock.Setup(v => v.GetVendorById(It.IsAny<string>())).ReturnsAsync(new Vendor { Id = "vendor-1" });
        var globalScope = new GlobalAdminDataScope<VendorReview>();

        await _service.DisapproveVendorReviews(new[] { "review-1:vendor-WRONG" }, globalScope);

        _vendorServiceMock.Verify(v => v.GetVendorById("vendor-1"), Times.Once);
        _vendorServiceMock.Verify(v => v.GetVendorById("vendor-WRONG"), Times.Never);
    }
}
