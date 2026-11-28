using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerNoteViewModelServiceTests
{
    private Mock<ICustomerNoteService> _customerNoteServiceMock;
    private Mock<IDownloadService> _downloadServiceMock;
    private CustomerNoteViewModelService _customerNoteViewModelService;

    public CustomerNoteViewModelServiceTests()
    {
        _customerNoteServiceMock = new Mock<ICustomerNoteService>();
        _downloadServiceMock = new Mock<IDownloadService>();
        _customerNoteServiceMock.Setup(c => c.InsertCustomerNote(It.IsAny<CustomerNote>()))
            .Returns(Task.CompletedTask);

        _customerNoteViewModelService = new CustomerNoteViewModelService(
            _customerNoteServiceMock.Object,
            new Mock<ICustomerService>().Object,
            _downloadServiceMock.Object,
            new Mock<ICustomerMessageProviderService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IContextAccessor>().Object);
    }

    [TestMethod]
    public async Task InsertCustomerNote_InsertsNoteWithMappedFields()
    {
        _downloadServiceMock.Setup(d => d.GetDownloadById("d1")).ReturnsAsync(new Download
            { Id = "d1", DownloadType = DownloadType.Customer, ReferenceId = "c1" });

        var result = await _customerNoteViewModelService.InsertCustomerNote("c1", "d1", false, "title", "message");

        Assert.IsTrue(result);
        _customerNoteServiceMock.Verify(c => c.InsertCustomerNote(It.Is<CustomerNote>(x =>
                x.CustomerId == "c1" && x.DownloadId == "d1" && x.Title == "title" && x.Note == "message" &&
                !x.DisplayToCustomer)),
            Times.Once);
    }

    [TestMethod]
    public async Task InsertCustomerNote_FileOfAnotherRecord_Refused_NothingInserted()
    {
        _downloadServiceMock.Setup(d => d.GetDownloadById("d1")).ReturnsAsync(new Download
            { Id = "d1", DownloadType = DownloadType.Product, ReferenceId = "c1" });

        var result = await _customerNoteViewModelService.InsertCustomerNote("c1", "d1", false, "title", "message");

        Assert.IsFalse(result);
        _customerNoteServiceMock.Verify(c => c.InsertCustomerNote(It.IsAny<CustomerNote>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertCustomerNote_FileOfAnotherCustomer_Refused_NothingInserted()
    {
        _downloadServiceMock.Setup(d => d.GetDownloadById("d1")).ReturnsAsync(new Download
            { Id = "d1", DownloadType = DownloadType.Customer, ReferenceId = "c2" });

        var result = await _customerNoteViewModelService.InsertCustomerNote("c1", "d1", false, "title", "message");

        Assert.IsFalse(result);
        _customerNoteServiceMock.Verify(c => c.InsertCustomerNote(It.IsAny<CustomerNote>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteCustomerNote_DeletesNoteAndAttachment()
    {
        var note = new CustomerNote { Id = "n1", CustomerId = "c1", DownloadId = "d1" };
        var download = new Download { Id = "d1", DownloadType = DownloadType.Customer, ReferenceId = "c1" };
        _customerNoteServiceMock.Setup(c => c.GetCustomerNote("n1")).ReturnsAsync(note);
        _customerNoteServiceMock.Setup(c => c.DeleteCustomerNote(It.IsAny<CustomerNote>())).Returns(Task.CompletedTask);
        _downloadServiceMock.Setup(d => d.GetDownloadById("d1")).ReturnsAsync(download);
        _downloadServiceMock.Setup(d => d.DeleteDownload(It.IsAny<Download>())).Returns(Task.CompletedTask);

        await _customerNoteViewModelService.DeleteCustomerNote("n1", "c1");

        _customerNoteServiceMock.Verify(c => c.DeleteCustomerNote(note), Times.Once);
        _downloadServiceMock.Verify(d => d.DeleteDownload(download), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCustomerNote_FileOfAnotherRecord_NoteDeleted_FileKept()
    {
        var note = new CustomerNote { Id = "n1", CustomerId = "c1", DownloadId = "d1" };
        _customerNoteServiceMock.Setup(c => c.GetCustomerNote("n1")).ReturnsAsync(note);
        _customerNoteServiceMock.Setup(c => c.DeleteCustomerNote(It.IsAny<CustomerNote>())).Returns(Task.CompletedTask);
        _downloadServiceMock.Setup(d => d.GetDownloadById("d1")).ReturnsAsync(new Download
            { Id = "d1", DownloadType = DownloadType.Product, ReferenceId = "p1" });

        await _customerNoteViewModelService.DeleteCustomerNote("n1", "c1");

        _customerNoteServiceMock.Verify(c => c.DeleteCustomerNote(note), Times.Once);
        _downloadServiceMock.Verify(d => d.DeleteDownload(It.IsAny<Download>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteCustomerNote_NotFound_Throws()
    {
        _customerNoteServiceMock.Setup(c => c.GetCustomerNote(It.IsAny<string>()))
            .ReturnsAsync((CustomerNote?)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerNoteViewModelService.DeleteCustomerNote("missing", "c1"));
    }

    [TestMethod]
    public async Task DeleteCustomerNote_NoteOfAnotherCustomer_Throws_NothingDeleted()
    {
        var note = new CustomerNote { Id = "n1", CustomerId = "c2", DownloadId = "d1" };
        _customerNoteServiceMock.Setup(c => c.GetCustomerNote("n1")).ReturnsAsync(note);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerNoteViewModelService.DeleteCustomerNote("n1", "c1"));
        _customerNoteServiceMock.Verify(c => c.DeleteCustomerNote(It.IsAny<CustomerNote>()), Times.Never);
        _downloadServiceMock.Verify(d => d.DeleteDownload(It.IsAny<Download>()), Times.Never);
    }
}
