using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Events;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Tests.Services
{
    [TestClass]
    public class LiquidObjectBuilderTests
    {
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [TestMethod]
        public async Task BuilderTest()
        {
            var builder = new LiquidObjectBuilder(_mediatorMock.Object);
            var vendor = new Vendor() { Id = "VendorId",Name="vendorName" };
            var product= new Product() { Id = "productId", Name = "ProductName" };
            var vendorReview = new VendorReview() { Id = "RevId" };
            _mediatorMock.Setup(c => c.Send<LiquidVendor>(It.IsAny<IRequest<LiquidVendor>>(), default)).ReturnsAsync(new LiquidVendor(vendor));

            var liquidObject = await builder
                .AddVendorReviewTokens(vendor, vendorReview)
                .AddVendorTokens(vendor, new Language())
                .AddOutOfStockTokens(product, new OutOfStockSubscription(),new Store(),new Language())
                .BuildAsync();


            Assert.IsTrue(liquidObject.Vendor != null);
            Assert.IsTrue((liquidObject.Vendor as LiquidVendor).Name.Equals(vendor.Name));
            Assert.IsNotNull(liquidObject.VendorReview as LiquidVendorReview);
            Assert.IsTrue((liquidObject.VendorReview as LiquidVendorReview).VendorName.Equals(vendor.Name));
            Assert.IsTrue((liquidObject.OutOfStockSubscription as LiquidOutOfStockSubscription).ProductName.Equals(product.Name));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<INotification>(), default), Times.Exactly(3));
        }
    }
}
