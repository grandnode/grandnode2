﻿using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Customers.Services;
using Grand.Domain.Affiliates;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services
{
    [TestClass()]
    public class AffiliateServiceTests
    {
        private Mock<IRepository<Affiliate>> _affiliateRepository;
        private Mock<IRepository<Order>> _orderRepository;
        private Mock<IMediator> _mediatorMock;
        private IAffiliateService _affiliateService;

        [TestInitialize()]
        public void TestInitialize()
        {
            _affiliateRepository = new Mock<IRepository<Affiliate>>();
            _orderRepository = new Mock<IRepository<Order>>();
            _mediatorMock = new Mock<IMediator>();
            _affiliateService = new AffiliateService(_affiliateRepository.Object, _orderRepository.Object, _mediatorMock.Object);
        }
        [TestMethod()]
        public async Task GetAffiliateByIdTest()
        {
            await _affiliateService.GetAffiliateById("");
            _affiliateRepository.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
           
        }

        [TestMethod()]
        public async Task UpdateAffiliate_NullParameter_ThrwoException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _affiliateService.UpdateAffiliate(null), "affiliate");
        }

        [TestMethod()]
        public async Task InsertAffiliate_NullParameter_ThrwoException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _affiliateService.InsertAffiliate(null), "affiliate");
        }

        [TestMethod()]
        public async Task DeleteAffiliate_NullParameter_ThrwoException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _affiliateService.DeleteAffiliate(null), "affiliate");
        }


        [TestMethod()]
        public async Task UpdateAffiliate_ValidParamters()
        {
            var affiliate = new Affiliate();
            await _affiliateService.UpdateAffiliate(affiliate);
            _affiliateRepository.Verify(c => c.UpdateAsync(It.IsAny<Affiliate>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Affiliate>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task InsertAffiliate_ValidParamters()
        {
            var affiliate = new Affiliate();
            await _affiliateService.InsertAffiliate(affiliate);
            _affiliateRepository.Verify(c => c.InsertAsync(It.IsAny<Affiliate>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Affiliate>>(), default(CancellationToken)), Times.Once);
        }


    }
}
