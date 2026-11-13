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
    private List<MerchandiseReturnNote> _notes;

    [TestInitialize]
    public void Init()
    {
        _notes = [];
        var merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        merchandiseReturnService.Setup(x => x.GetMerchandiseReturnNotes(It.IsAny<string>()))
            .ReturnsAsync(() => _notes);
        _handler = new GetMerchandiseReturnDetailsHandler(new Mock<IProductService>().Object,
            merchandiseReturnService.Object, new Mock<IPriceFormatter>().Object, new Mock<IContextAccessor>().Object,
            new Mock<IMediator>().Object, new Mock<IDateTimeService>().Object, new OrderSettings(),
            new Mock<IReturnShippingService>().Object, new Mock<ICurrencyService>().Object,
            new Mock<ITranslationService>().Object, new Mock<ICountryService>().Object,
            NullLogger<GetMerchandiseReturnDetailsHandler>.Instance);
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
    public async Task Details_NotesEncoded()
    {
        //what a customer typed is text: no markup, no template, whoever reads it
        _notes.Add(new MerchandiseReturnNote {
            Id = "n2", Note = "<script>x</script>\r\n{{ a }}", DisplayToCustomer = true, CreatedByCustomer = true,
            CreatedOnUtc = new DateTime(2026, 11, 2)
        });
        _notes.Add(new MerchandiseReturnNote
            { Id = "n1", Note = "Accepted", DisplayToCustomer = true, CreatedOnUtc = new DateTime(2026, 11, 1) });
        _notes.Add(new MerchandiseReturnNote
            { Id = "n0", Note = "staff only", DisplayToCustomer = false, CreatedOnUtc = new DateTime(2026, 11, 1) });

        var model = await _handler.Handle(new GetMerchandiseReturnDetails {
            MerchandiseReturn = new MerchandiseReturn { Id = "r1" }, Order = new Order { Id = "o1" },
            Language = new Language { Id = "l1" }
        }, CancellationToken.None);

        //oldest first, as a conversation reads
        CollectionAssert.AreEqual(new[] { "n1", "n2" }, model.MerchandiseReturnNotes.Select(x => x.Id).ToArray());
        Assert.AreEqual("&lt;script&gt;x&lt;/script&gt;<br />{{ a }}", model.MerchandiseReturnNotes[1].Note);
        Assert.IsTrue(model.MerchandiseReturnNotes[1].CreatedByCustomer);
        Assert.IsFalse(model.MerchandiseReturnNotes[0].CreatedByCustomer);
    }

    [TestMethod]
    public async Task CancelledLabel_InstructionsAndCodeHidden()
    {
        var shipping = await Shipping(MerchandiseReturnStatus.Pending, ReturnLabelStatus.Cancelled);

        Assert.IsNull(shipping.Instructions);
        Assert.IsNull(shipping.ReturnCode);
    }
}
