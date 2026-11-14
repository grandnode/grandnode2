using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Orders;

[TestClass]
public class InsertMerchandiseReturnNoteCommandHandlerTests
{
    private readonly Order _order = new() { Id = "o1", StoreId = "s1" };
    private Mock<IDownloadService> _downloads;
    private InsertMerchandiseReturnNoteCommandHandler _handler;
    private Mock<IMerchandiseReturnMessageProviderService> _messages;
    private List<MerchandiseReturnNote> _notes;
    private OrderSettings _orderSettings;
    private Mock<IMerchandiseReturnService> _returns;

    [TestInitialize]
    public void Init()
    {
        _notes = [];
        _returns = new Mock<IMerchandiseReturnService>();
        _returns.Setup(x => x.GetMerchandiseReturnNotes("r1")).ReturnsAsync(() => _notes);
        _downloads = new Mock<IDownloadService>();
        _downloads.Setup(x => x.InsertDownload(It.IsAny<Download>()))
            .Callback<Download>(x => x.Id = "d1").Returns(Task.CompletedTask);
        _messages = new Mock<IMerchandiseReturnMessageProviderService>();
        _orderSettings = new OrderSettings { MerchandiseReturns_AllowToUploadFiles = true };
        _handler = new InsertMerchandiseReturnNoteCommandHandler(_returns.Object, _downloads.Object,
            _messages.Object, new MediaSettings(), new LanguageSettings { DefaultAdminLanguageId = "admin" },
            _orderSettings, NullLogger<InsertMerchandiseReturnNoteCommandHandler>.Instance);
    }

    private Task<string> Handle(MerchandiseReturnStatus status, string note, IFormFile file = null)
    {
        return _handler.Handle(new InsertMerchandiseReturnNoteCommand {
            MerchandiseReturn = new MerchandiseReturn { Id = "r1", MerchandiseReturnStatus = status },
            Order = _order, Note = note, File = file, Customer = new Customer { Id = "c1" },
            Host = new DomainHost { HostName = "host" }
        }, CancellationToken.None);
    }

    private static IFormFile File(string name)
    {
        return new FormFile(new MemoryStream(new byte[10]), 0, 10, "file", name) { Headers = new HeaderDictionary() };
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Completed)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    public async Task AddNote_ClosedStatus_Refused(MerchandiseReturnStatus status)
    {
        var error = await Handle(status, "Hello");

        Assert.AreEqual("MerchandiseReturns.Notes.Closed", error);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()), Times.Never);
    }

    [TestMethod]
    public async Task AddNote_Empty_Refused()
    {
        var error = await Handle(MerchandiseReturnStatus.Pending, "   ");

        Assert.AreEqual("MerchandiseReturns.Notes.Required", error);
    }

    [TestMethod]
    public async Task AddNote_TooLong_Refused()
    {
        var error = await Handle(MerchandiseReturnStatus.Pending, " " + new string('a', 2001) + " ");

        Assert.AreEqual("MerchandiseReturns.Notes.TooLong", error);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()), Times.Never);
    }

    [TestMethod]
    public async Task AddNote_BadAttachment_Refused()
    {
        var error = await Handle(MerchandiseReturnStatus.Pending, "See the file", File("invoice.exe"));

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", error);
        _downloads.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()), Times.Never);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Pending)]
    [DataRow(MerchandiseReturnStatus.Accepted)]
    [DataRow(MerchandiseReturnStatus.Received)]
    public async Task AddNote_Open_InsertsCustomerNoteAndNotifiesStore(MerchandiseReturnStatus status)
    {
        var error = await Handle(status, "  <b>Is it broken?</b>  ", File("crack.jpg"));

        Assert.IsNull(error);
        //stored as typed: every page and e-mail encodes it when it shows it
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.MerchandiseReturnId == "r1" && n.Note == "<b>Is it broken?</b>" && n.CreatedByCustomer &&
            n.DisplayToCustomer && n.DownloadId == "d1")), Times.Once);
        _downloads.Verify(x => x.InsertDownload(It.Is<Download>(d =>
            d.ReferenceId == "r1" && d.CustomerId == "c1" && d.ContentType == "image/jpeg")), Times.Once);
        _messages.Verify(x => x.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage(
            It.Is<MerchandiseReturn>(r => r.Id == "r1"), It.IsAny<MerchandiseReturnNote>(), _order, "admin",
            It.Is<DomainHost>(h => h.HostName == "host")), Times.Once);
    }

    [TestMethod]
    public async Task AddNote_UploadsOff_SavesTheMessageWithoutTheFile()
    {
        _orderSettings.MerchandiseReturns_AllowToUploadFiles = false;

        //a crafted post, or a page opened before the store turned uploads off
        var error = await Handle(MerchandiseReturnStatus.Pending, "Hello", File("crack.jpg"));

        Assert.IsNull(error);
        _downloads.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.Note == "Hello" && n.DownloadId == null)), Times.Once);
    }

    [TestMethod]
    public async Task AddNote_RightAfterTheLast_Refused()
    {
        //a second click on Send
        _notes.Add(new MerchandiseReturnNote { CreatedByCustomer = true, CreatedOnUtc = DateTime.UtcNow.AddSeconds(-2) });

        var error = await Handle(MerchandiseReturnStatus.Pending, "Hello");

        Assert.AreEqual("MerchandiseReturns.Notes.TooSoon", error);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()), Times.Never);
        _messages.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task AddNote_RightAfterTheStores_Inserted()
    {
        _notes.Add(new MerchandiseReturnNote { CreatedOnUtc = DateTime.UtcNow.AddSeconds(-2) });
        _notes.Add(new MerchandiseReturnNote { CreatedByCustomer = true, CreatedOnUtc = DateTime.UtcNow.AddMinutes(-5) });

        var error = await Handle(MerchandiseReturnStatus.Pending, "Thanks");

        Assert.IsNull(error);
    }

    [TestMethod]
    public async Task AddNote_TooManyOnTheReturn_Refused()
    {
        for (var i = 0; i < MerchandiseReturnUploads.MaxCustomerNotes; i++)
            _notes.Add(new MerchandiseReturnNote { CreatedByCustomer = true, CreatedOnUtc = DateTime.UtcNow.AddDays(-1) });

        var error = await Handle(MerchandiseReturnStatus.Pending, "Hello");

        Assert.AreEqual("MerchandiseReturns.Notes.TooMany", error);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()), Times.Never);
    }

    [TestMethod]
    public async Task AddNote_EmailFails_NoteStandsWithoutAnError()
    {
        _messages.Setup(x => x.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage(It.IsAny<MerchandiseReturn>(),
                It.IsAny<MerchandiseReturnNote>(), It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<DomainHost>()))
            .ThrowsAsync(new InvalidOperationException("no e-mail account"));

        var error = await Handle(MerchandiseReturnStatus.Pending, "Hello");

        Assert.IsNull(error);
        _returns.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()), Times.Once);
    }
}
