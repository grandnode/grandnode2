using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class OrderNoteViewModelServiceTests
{
    private Mock<IOrderService> _orderService;
    private Mock<IDownloadService> _downloadService;
    private OrderNoteViewModelService _service;
    private readonly Order _order = new() { Id = "o1" };

    [TestInitialize]
    public void Setup()
    {
        _orderService = new Mock<IOrderService>();
        _downloadService = new Mock<IDownloadService>();
        _service = new OrderNoteViewModelService(_orderService.Object, _downloadService.Object,
            new Mock<IDateTimeService>().Object, new Mock<IOrderMessageProviderService>().Object);
    }

    [TestMethod]
    public async Task InsertOrderNote_FileOfThisOrder_IsAttached()
    {
        _downloadService.Setup(s => s.GetDownloadById("d1"))
            .ReturnsAsync(new Download { Id = "d1", DownloadType = DownloadType.Order, ReferenceId = "o1" });

        var saved = await _service.InsertOrderNote(_order, "d1", false, "msg");

        Assert.IsTrue(saved);
        _orderService.Verify(s => s.InsertOrderNote(It.Is<OrderNote>(n => n.DownloadId == "d1")), Times.Once);
    }

    [TestMethod]
    [DataRow(DownloadType.Product, "o1")]
    [DataRow(DownloadType.Order, "o2")]
    public async Task InsertOrderNote_FileNotOfThisOrder_IsRefused_NothingSaved(DownloadType type, string referenceId)
    {
        _downloadService.Setup(s => s.GetDownloadById("d1"))
            .ReturnsAsync(new Download { Id = "d1", DownloadType = type, ReferenceId = referenceId });

        var saved = await _service.InsertOrderNote(_order, "d1", true, "msg");

        Assert.IsFalse(saved);
        _orderService.Verify(s => s.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertOrderNote_WithoutFile_IsSaved()
    {
        var saved = await _service.InsertOrderNote(_order, "", false, "msg");

        Assert.IsTrue(saved);
        _orderService.Verify(s => s.InsertOrderNote(It.Is<OrderNote>(n => n.Note == "msg")), Times.Once);
        _downloadService.Verify(s => s.GetDownloadById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteOrderNote_FileOfThisOrder_IsDeleted()
    {
        var download = new Download { Id = "d1", DownloadType = DownloadType.Order, ReferenceId = "o1" };
        NoteWithDownload(download);

        await _service.DeleteOrderNote(_order, "n1");

        _downloadService.Verify(s => s.DeleteDownload(download), Times.Once);
    }

    [TestMethod]
    public async Task DeleteOrderNote_FileNotOfThisOrder_IsKept()
    {
        //e.g. a product file whose id was put into the note by hand
        NoteWithDownload(new Download { Id = "d1", DownloadType = DownloadType.Product, ReferenceId = "p1" });

        await _service.DeleteOrderNote(_order, "n1");

        _orderService.Verify(s => s.DeleteOrderNote(It.Is<OrderNote>(n => n.Id == "n1")), Times.Once);
        _downloadService.Verify(s => s.DeleteDownload(It.IsAny<Download>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareOrderNotes_NoteWithoutFile_DoesNotLookUpDownload()
    {
        _orderService.Setup(s => s.GetOrderNotes("o1"))
            .ReturnsAsync(new List<OrderNote> { new() { Id = "n1", OrderId = "o1", DownloadId = "" } });

        var notes = await _service.PrepareOrderNotes(_order);

        Assert.AreEqual(1, notes.Count);
        _downloadService.Verify(s => s.GetDownloadById(It.IsAny<string>()), Times.Never);
    }

    private void NoteWithDownload(Download download)
    {
        _orderService.Setup(s => s.GetOrderNotes("o1"))
            .ReturnsAsync(new List<OrderNote> { new() { Id = "n1", OrderId = "o1", DownloadId = download.Id } });
        _downloadService.Setup(s => s.GetDownloadById(download.Id)).ReturnsAsync(download);
    }
}
