using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Services.GiftVouchers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
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

namespace Grand.Business.Checkout.Tests.Services.GiftVouchers
{
    [TestClass]
    public class GiftVoucherServiceTests
    {
        private Mock<IRepository<GiftVoucher>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private IGiftVoucherService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<GiftVoucher>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new GiftVoucherService(_repositoryMock.Object, _mediatorMock.Object);
        }

        [TestMethod]
        public void GenerateGiftVoucherCode_ShouldReturnUniqueValues()
        {
            var results = new List<string>();
            results.Add(_service.GenerateGiftVoucherCode());
            results.Add(_service.GenerateGiftVoucherCode());
            results.Add(_service.GenerateGiftVoucherCode());
            results.Add(_service.GenerateGiftVoucherCode());
            results.Add(_service.GenerateGiftVoucherCode());
            results.Add(_service.GenerateGiftVoucherCode());
            results.Add(_service.GenerateGiftVoucherCode());

            Assert.IsTrue(results.Count.Equals(results.Distinct().Count()));
        }

        [TestMethod]
        public async Task InsertGiftVoucher_InovokeExpectedMethods()
        {
            await _service.InsertGiftVoucher(new GiftVoucher() { Code="code"});
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<GiftVoucher>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<GiftVoucher>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertGiftVoucher_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertGiftVoucher(null));
        }

        [TestMethod]
        public async Task UpdateGiftVoucher_InovokeExpectedMethods()
        {
            await _service.UpdateGiftVoucher(new GiftVoucher() { Code = "code" });
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<GiftVoucher>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<GiftVoucher>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdateGiftVoucher_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateGiftVoucher(null));
        }

        [TestMethod]
        public async Task DeleteGiftVoucher_InovokeExpectedMethods()
        {
            await _service.DeleteGiftVoucher(new GiftVoucher());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<GiftVoucher>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<GiftVoucher>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeleteGiftVoucher_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteGiftVoucher(null));
        }
    }
}
