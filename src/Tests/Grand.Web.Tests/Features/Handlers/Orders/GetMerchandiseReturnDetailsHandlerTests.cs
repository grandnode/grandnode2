using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetMerchandiseReturnDetailsHandlerTests
{
    private GetMerchandiseReturnDetailsHandler _handler;

    [TestInitialize]
    public void Init()
    {
        var merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        merchandiseReturnService.Setup(x => x.GetMerchandiseReturnNotes(It.IsAny<string>()))
            .ReturnsAsync(new List<MerchandiseReturnNote>());
        _handler = new GetMerchandiseReturnDetailsHandler(new Mock<IProductService>().Object,
            merchandiseReturnService.Object, new Mock<IPriceFormatter>().Object, new Mock<IContextAccessor>().Object,
            new Mock<IMediator>().Object, new Mock<IDateTimeService>().Object, new OrderSettings(),
            new Mock<IReturnShippingService>().Object, new Mock<ICurrencyService>().Object,
            new Mock<ITranslationService>().Object, NullLogger<GetMerchandiseReturnDetailsHandler>.Instance);
    }

    private async Task<Grand.Web.Models.Orders.MerchandiseReturnDetailsModel.ReturnShippingModel> Shipping(
        MerchandiseReturnStatus status, ReturnLabelStatus labelStatus)
    {
        var merchandiseReturn = new MerchandiseReturn {
            Id = "r1", MerchandiseReturnStatus = status,
            ReturnShipping = new MerchandiseReturnShipping {
                ProviderSystemName = "Return.Point", OptionName = "Point", Instructions = "Bring code C1",
                ReturnCode = "C1", LabelStatus = labelStatus, LabelDownloadId = "d1"
            }
        };
        var model = await _handler.Handle(new GetMerchandiseReturnDetails {
            MerchandiseReturn = merchandiseReturn, Order = new Order { Id = "o1" }, Language = new Language { Id = "l1" }
        }, CancellationToken.None);
        return model.ReturnShipping;
    }

    [TestMethod]
    public async Task OpenReturn_InstructionsAndCodeShown()
    {
        var shipping = await Shipping(MerchandiseReturnStatus.Accepted, ReturnLabelStatus.Created);

        Assert.AreEqual("Bring code C1", shipping.Instructions);
        Assert.AreEqual("C1", shipping.ReturnCode);
        Assert.IsTrue(shipping.HasLabel);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    public async Task ClosedReturn_LabelTheCarrierDidNotCancel_NotOffered(MerchandiseReturnStatus status)
    {
        var shipping = await Shipping(status, ReturnLabelStatus.Created);

        Assert.IsFalse(shipping.HasLabel);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Received)]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    [DataRow(MerchandiseReturnStatus.Completed)]
    public async Task ClosedReturn_InstructionsAndCodeHidden(MerchandiseReturnStatus status)
    {
        var shipping = await Shipping(status, ReturnLabelStatus.Created);

        Assert.IsNull(shipping.Instructions);
        Assert.IsNull(shipping.ReturnCode);
    }

    [TestMethod]
    public async Task CancelledLabel_InstructionsAndCodeHidden()
    {
        var shipping = await Shipping(MerchandiseReturnStatus.Pending, ReturnLabelStatus.Cancelled);

        Assert.IsNull(shipping.Instructions);
        Assert.IsNull(shipping.ReturnCode);
    }
}
