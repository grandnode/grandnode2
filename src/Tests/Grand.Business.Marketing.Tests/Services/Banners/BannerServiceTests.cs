using Grand.Business.Marketing.Services.Banners;
using Grand.Domain.Data;
using Grand.Domain.Messages;
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

namespace Grand.Business.Marketing.Tests.Services.Banners
{
    [TestClass()]
    public class BannerServiceTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<IRepository<Banner>> _repositoryMock;
        private BannerService _bannerService;

        [TestInitialize()]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _repositoryMock = new Mock<IRepository<Banner>>();
            _bannerService = new BannerService(_repositoryMock.Object,_mediatorMock.Object);
        }


        [TestMethod()]
        public void DeleteBanner_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _bannerService.DeleteBanner(null), "banner");
        }

        [TestMethod()]
        public async Task Delete_ValidArgument_InvokeRepository()
        {
            await _bannerService.DeleteBanner(new Banner());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Banner>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Banner>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public void InsertBanner_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _bannerService.InsertBanner(null), "banner");
        }

        [TestMethod()]
        public async Task InsertBanner_ValidArgument_InvokeRepository()
        {
            await _bannerService.InsertBanner(new Banner());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Banner>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Banner>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public void UpdateBanner_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _bannerService.UpdateBanner(null), "banner");
        }

        [TestMethod()]
        public async Task UpdateBanner_ValidArgument_InvokeRepository()
        {
            await _bannerService.UpdateBanner(new Banner());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Banner>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Banner>>(), default(CancellationToken)), Times.Once);
        }
    }
}
