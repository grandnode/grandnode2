using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class LiquidObjectBuilderTests
{
    private Mock<IMediator> _mediatorMock;

    public LiquidObjectBuilderTests()
    {
        _mediatorMock = new Mock<IMediator>();
    }

    [TestMethod]
    public async Task BuilderTest()
    {
        var builder = new LiquidObjectBuilder(_mediatorMock.Object);
        var vendor = new Vendor { Id = "VendorId", Name = "vendorName" };
        var product = new Product { Id = "productId", Name = "ProductName" };
        var vendorReview = new VendorReview { Id = "RevId" };
        _mediatorMock.Setup(c => c.Send(It.IsAny<IRequest<LiquidVendor>>(), default))
            .ReturnsAsync(new LiquidVendor(vendor));

        var liquidObject = await builder
            .AddVendorReviewTokens(vendor, vendorReview)
            .AddVendorTokens(vendor, new Language())
            .AddOutOfStockTokens(product, new OutOfStockSubscription(), new Store(),
                new DomainHost { Url = "https://localhost:44350/" }, new Language())
            .BuildAsync();


        Assert.IsNotNull(liquidObject.Vendor);
        Assert.IsTrue(string.Equals(Assert.IsInstanceOfType<LiquidVendor>(liquidObject.Vendor).Name, vendor.Name));
        Assert.IsNotNull(liquidObject.VendorReview as LiquidVendorReview);
        Assert.IsTrue(string.Equals(Assert.IsInstanceOfType<LiquidVendorReview>(liquidObject.VendorReview).VendorName, vendor.Name));
        Assert.IsTrue(
            string.Equals(Assert.IsInstanceOfType<LiquidOutOfStockSubscription>(liquidObject.OutOfStockSubscription).ProductName, product.Name));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<INotification>(), default), Times.Exactly(3));
    }
}