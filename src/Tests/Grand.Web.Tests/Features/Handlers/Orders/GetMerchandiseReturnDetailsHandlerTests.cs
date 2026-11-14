using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;
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
        //the user's time is an hour ahead of UTC, and amounts are shown with their currency
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns((DateTime date, DateTimeKind _) => date.AddHours(1));
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource("MerchandiseReturns.Timeline.Submitted", "l1",
            It.IsAny<string>(), It.IsAny<bool>())).Returns("Submitted");
        translationService.Setup(x => x.GetResource("MerchandiseReturns.Timeline.Refunded", "l1",
            It.IsAny<string>(), It.IsAny<bool>())).Returns("Refunded {0}");
        _handler = new GetMerchandiseReturnDetailsHandler(new Mock<IProductService>().Object,
            merchandiseReturnService.Object, priceFormatter.Object, new Mock<IContextAccessor>().Object,
            new Mock<IMediator>().Object, dateTimeService.Object, new OrderSettings(),
            new Mock<IReturnShippingService>().Object, currencyService.Object,
            translationService.Object, new Mock<ICountryService>().Object,
            new HtmlSanitizationService(new SecurityConfig()),
            NullLogger<GetMerchandiseReturnDetailsHandler>.Instance);
    }

    private Task<Grand.Web.Models.Orders.MerchandiseReturnDetailsModel> Details(MerchandiseReturn merchandiseReturn)
    {
        return _handler.Handle(new GetMerchandiseReturnDetails {
            MerchandiseReturn = merchandiseReturn, Order = new Order { Id = "o1", CustomerCurrencyCode = "EUR" },
            Language = new Language { Id = "l1" }
        }, CancellationToken.None);
    }

    private static readonly DateTime Submitted = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Timeline_HistoryInOrder()
    {
        var merchandiseReturn = new MerchandiseReturn
            { Id = "r1", CreatedOnUtc = Submitted, MerchandiseReturnStatus = MerchandiseReturnStatus.Received };
        merchandiseReturn.StatusHistory.Add(new MerchandiseReturnStatusChange
            { Status = MerchandiseReturnStatus.Accepted, CreatedOnUtc = Submitted.AddDays(1) });
        merchandiseReturn.StatusHistory.Add(new MerchandiseReturnStatusChange
            { Status = MerchandiseReturnStatus.Received, CreatedOnUtc = Submitted.AddDays(3) });

        var model = await Details(merchandiseReturn);

        CollectionAssert.AreEqual(new[] { "Submitted", "Accepted", "Received" },
            model.Timeline.Select(x => x.Name).ToArray());
        CollectionAssert.AreEqual(new DateTime?[] { Submitted.AddHours(1), Submitted.AddDays(1).AddHours(1),
            Submitted.AddDays(3).AddHours(1) }, model.Timeline.Select(x => x.Date).ToArray());
    }

    [TestMethod]
    public async Task Timeline_OldReturnWithoutHistory_CurrentStatusUndated()
    {
        //changed before the history was recorded: its status is known, not when it was reached
        var model = await Details(new MerchandiseReturn
            { Id = "r1", CreatedOnUtc = Submitted, MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted });

        Assert.AreEqual(2, model.Timeline.Count);
        Assert.AreEqual("Accepted", model.Timeline[1].Name);
        Assert.IsNull(model.Timeline[1].Date);
    }

    [TestMethod]
    public async Task Timeline_PendingWithoutHistory_SubmittedOnly()
    {
        var model = await Details(new MerchandiseReturn { Id = "r1", CreatedOnUtc = Submitted });

        Assert.AreEqual("Submitted", model.Timeline.Single().Name);
    }

    [TestMethod]
    public async Task Timeline_Refunded_BeforeTheCompletionItLedTo()
    {
        //the refund is saved, then the return completed a moment later
        var merchandiseReturn = new MerchandiseReturn {
            Id = "r1", CreatedOnUtc = Submitted, MerchandiseReturnStatus = MerchandiseReturnStatus.Completed,
            Refund = new MerchandiseReturnRefund { Amount = 17, RefundedOnUtc = Submitted.AddDays(5) }
        };
        merchandiseReturn.StatusHistory.Add(new MerchandiseReturnStatusChange
            { Status = MerchandiseReturnStatus.Completed, CreatedOnUtc = Submitted.AddDays(5).AddSeconds(2) });

        var model = await Details(merchandiseReturn);

        CollectionAssert.AreEqual(new[] { "Submitted", "Refunded EUR17", "Completed" },
            model.Timeline.Select(x => x.Name).ToArray());
        Assert.AreEqual(Submitted.AddDays(5).AddHours(1), model.Timeline[1].Date);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Pending, true, true, true)]
    [DataRow(MerchandiseReturnStatus.Accepted, false, true, true)]
    [DataRow(MerchandiseReturnStatus.Received, false, true, false)]
    [DataRow(MerchandiseReturnStatus.Rejected, false, false, false)]
    [DataRow(MerchandiseReturnStatus.Cancelled, false, false, false)]
    [DataRow(MerchandiseReturnStatus.Completed, false, false, false)]
    public async Task Flags_ByStatus(MerchandiseReturnStatus status, bool canCancel, bool canAddNote,
        bool canPrintDocument)
    {
        var model = await Details(new MerchandiseReturn { Id = "r1", MerchandiseReturnStatus = status });

        Assert.AreEqual(canCancel, model.CanCancel);
        Assert.AreEqual(canAddNote, model.CanAddNote);
        Assert.AreEqual(canPrintDocument, model.CanPrintDocument);
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
    public async Task Details_StaffNoteMarkup_KeptWhenTheAllowlistKeepsIt()
    {
        //written before the notes were encoded: a link to a form still shows as one, a script does not run
        _notes.Add(new MerchandiseReturnNote {
            Id = "n1", Note = "Print <a href=\"https://shop.com/form.pdf\">this form</a>", DisplayToCustomer = true
        });
        _notes.Add(new MerchandiseReturnNote
            { Id = "n2", Note = "<img src=x onerror=alert(1)>", DisplayToCustomer = true });

        var model = await Details(new MerchandiseReturn { Id = "r1" });

        Assert.AreEqual("Print <a href=\"https://shop.com/form.pdf\">this form</a>",
            model.MerchandiseReturnNotes.Single(x => x.Id == "n1").Note);
        Assert.AreEqual("&lt;img src=x onerror=alert(1)&gt;", model.MerchandiseReturnNotes.Single(x => x.Id == "n2").Note);
    }

    [TestMethod]
    public async Task CancelledLabel_InstructionsAndCodeHidden()
    {
        var shipping = await Shipping(MerchandiseReturnStatus.Pending, ReturnLabelStatus.Cancelled);

        Assert.IsNull(shipping.Instructions);
        Assert.IsNull(shipping.ReturnCode);
    }
}
