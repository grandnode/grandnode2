using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class VendorServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<Vendor>> _repoMock;
    private Mock<IRepository<VendorReview>> _vendorReviewRepositoryMock;
    private VendorService _vendorService;

    [TestInitialize]
    public void Init()
    {
        _repoMock = new Mock<IRepository<Vendor>>();
        _vendorReviewRepositoryMock = new Mock<IRepository<VendorReview>>();
        _mediatorMock = new Mock<IMediator>();
        _vendorService = new VendorService(_repoMock.Object, _vendorReviewRepositoryMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetVendorByIdTest()
    {
        await _vendorService.GetVendorById("");
        _repoMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task GetVendorNoteById_Test()
    {
        await _vendorService.GetVendorNoteById("1", "1");
        _repoMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task InsertVendor_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.InsertVendor(new Vendor());
        _repoMock.Verify(c => c.InsertAsync(It.IsAny<Vendor>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Vendor>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteVendor_ValidArguments_SoftDeleteAndPublishEvent()
    {
        var vendor = new Vendor();
        await _vendorService.DeleteVendor(vendor);
        //soft delete
        Assert.IsTrue(vendor.Deleted);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Vendor>>(), default), Times.Once);
    }


    [TestMethod]
    public async Task UpdateVendor_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.UpdateVendor(new Vendor());
        _repoMock.Verify(c => c.UpdateAsync(It.IsAny<Vendor>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Vendor>>(), default), Times.Once);
    }


    [TestMethod]
    public async Task InsertVendorReview_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.InsertVendorReview(new VendorReview());
        _vendorReviewRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<VendorReview>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<VendorReview>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateVendorReview_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.UpdateVendorReview(new VendorReview());
        //_vendorReviewRepositoryMock.Verify(c => c.UpdateOneAsync(x=>x.Id == It.IsAny<string>(), It.IsAny<UpdateBuilder<VendorReview>>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<VendorReview>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateVendorReviewTotal_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.UpdateVendorReviewTotals(new Vendor());
        //_vendorReviewRepositoryMock.Verify(c => c.UpdateOneAsync(x=>x.Id == It.IsAny<string>(), It.IsAny<UpdateBuilder<VendorReview>>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Vendor>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteVendorReview_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.DeleteVendorReview(new VendorReview());
        _vendorReviewRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<VendorReview>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<VendorReview>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task InsertVendorNote_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _vendorService.InsertVendorNote(new VendorNote(), "1");
        _repoMock.Verify(c => c.AddToSet(It.IsAny<string>(), x => x.VendorNotes, It.IsAny<VendorNote>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<VendorNote>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteVendorNote_ValidArguments_SoftDeleteAndPublishEvent()
    {
        await _vendorService.DeleteVendorNote(new VendorNote(), "1");
        _repoMock.Verify(c => c.PullFilter(It.IsAny<string>(), x => x.VendorNotes, x => x.Id, It.IsAny<string>()),
            Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<VendorNote>>(), default), Times.Once);
    }
}