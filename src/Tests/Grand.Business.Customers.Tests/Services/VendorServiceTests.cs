using Grand.Business.Customers.Services;
using Grand.Domain.Data;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Tests.Services
{
    [TestClass()]
    public class VendorServiceTests
    {
        private Mock<IRepository<Vendor>> _repoMock;
        private Mock<IRepository<VendorReview>> _vendorReviewRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private VendorService _vendorService;

        [TestInitialize()]
        public void Init()
        {
            _repoMock = new Mock<IRepository<Vendor>>();
            _vendorReviewRepositoryMock = new Mock<IRepository<VendorReview>>();
            _mediatorMock = new Mock<IMediator>();
            _vendorService = new VendorService(_repoMock.Object,_vendorReviewRepositoryMock.Object,_mediatorMock.Object);
        }

        [TestMethod()]
        public async Task InsertVendor_ValidArguments_InvokeRepositoryAndPublishEvent()
        {
            await _vendorService.InsertVendor(new Vendor());
            _repoMock.Verify(c => c.InsertAsync(It.IsAny<Vendor>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Vendor>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteVendor_ValidArguments_SoftDeleteAndPublishEvent()
        {
            var vendor = new Vendor();
            await _vendorService.DeleteVendor(vendor);
            //soft delete
            Assert.IsTrue(vendor.Deleted);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Vendor>>(), default(CancellationToken)), Times.Once);
        }


        [TestMethod()]
        public async Task UpdateVendor_ValidArguments_InvokeRepositoryAndPublishEvent()
        {
            await _vendorService.UpdateVendor(new Vendor());
            _repoMock.Verify(c => c.UpdateAsync(It.IsAny<Vendor>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Vendor>>(), default(CancellationToken)), Times.Once);
        }


        [TestMethod()]
        public async Task InsertVendorReview_ValidArguments_InvokeRepositoryAndPublishEvent()
        {
            await _vendorService.InsertVendorReview(new VendorReview());
            _vendorReviewRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<VendorReview>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<VendorReview>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteVendorReview_ValidArguments_InvokeRepositoryAndPublishEvent()
        {
            await _vendorService.DeleteVendorReview(new VendorReview());
            _vendorReviewRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<VendorReview>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<VendorReview>>(), default(CancellationToken)), Times.Once);
        }
    }
}
