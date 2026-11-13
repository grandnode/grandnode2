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
using Microsoft.AspNetCore.Http;
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
    private Mock<IMerchandiseReturnService> _returns;

    [TestInitialize]
    public void Init()
    {
        _returns = new Mock<IMerchandiseReturnService>();
        _downloads = new Mock<IDownloadService>();
        _downloads.Setup(x => x.InsertDownload(It.IsAny<Download>()))
            .Callback<Download>(x => x.Id = "d1").Returns(Task.CompletedTask);
        _messages = new Mock<IMerchandiseReturnMessageProviderService>();
        _handler = new InsertMerchandiseReturnNoteCommandHandler(_returns.Object, _downloads.Object,
            _messages.Object, new MediaSettings(), new LanguageSettings { DefaultAdminLanguageId = "admin" });
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
}
