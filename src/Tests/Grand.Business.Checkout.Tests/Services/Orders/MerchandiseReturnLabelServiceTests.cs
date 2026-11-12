using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class MerchandiseReturnLabelServiceTests
{
    private const string Carrier = "Carrier";
    private static readonly string LabelKey = MerchandiseReturnExtensions.LabelLockKey("r1");
    private static readonly string OrderKey = MerchandiseReturnExtensions.QuantityLockKey("o1");

    private readonly List<string> _acquired = [];
    private readonly HashSet<string> _held = [];

    private Mock<IDownloadService> _downloadService;
    private Mock<IMerchandiseReturnService> _merchandiseReturnService;
    private Mock<IReturnShippingProvider> _provider;
    private Mock<IResourceLock> _resourceLock;
    private Mock<IReturnShippingService> _returnShippingService;
    private MerchandiseReturnLabelService _service;
    private MerchandiseReturn _stored;

    [TestInitialize]
    public void Init()
    {
        _stored = Return(ReturnLabelStatus.Pending);

        _merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById("r1")).ReturnsAsync(() => _stored);
        _provider = new Mock<IReturnShippingProvider>();
        _returnShippingService = new Mock<IReturnShippingService>();
        _returnShippingService.Setup(x => x.LoadProviderBySystemName(Carrier)).Returns(_provider.Object);
        _downloadService = new Mock<IDownloadService>();
        _downloadService.Setup(x => x.InsertDownload(It.IsAny<Download>()))
            .Callback<Download>(x => x.Id = "d1")
            .Returns(Task.CompletedTask);
        _resourceLock = new Mock<IResourceLock>();
        //records the keys in the order they are taken, and which are held at any time
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> keys, CancellationToken _) =>
            {
                var taken = keys.ToList();
                _acquired.AddRange(taken);
                _held.UnionWith(taken);
                return Task.FromResult<IAsyncDisposable>(new Handle(() => _held.ExceptWith(taken)));
            });

        _service = new MerchandiseReturnLabelService(_merchandiseReturnService.Object,
            _returnShippingService.Object, _downloadService.Object, _resourceLock.Object,
            NullLogger<MerchandiseReturnLabelService>.Instance);
    }

    private sealed class Handle(Action release) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            release();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ShortTimeoutLabelService(
        IMerchandiseReturnService merchandiseReturnService,
        IReturnShippingService returnShippingService,
        IDownloadService downloadService,
        IResourceLock resourceLock) : MerchandiseReturnLabelService(merchandiseReturnService, returnShippingService,
        downloadService, resourceLock, NullLogger<MerchandiseReturnLabelService>.Instance)
    {
        protected override TimeSpan ProviderTimeout => TimeSpan.FromMilliseconds(50);
    }

    private ShortTimeoutLabelService ShortTimeoutService()
    {
        return new ShortTimeoutLabelService(_merchandiseReturnService.Object, _returnShippingService.Object,
            _downloadService.Object, _resourceLock.Object);
    }

    private static MerchandiseReturn Return(ReturnLabelStatus labelStatus,
        MerchandiseReturnStatus status = MerchandiseReturnStatus.Pending)
    {
        return new MerchandiseReturn {
            Id = "r1",
            OrderId = "o1",
            CustomerId = "c1",
            MerchandiseReturnStatus = status,
            ReturnShipping = new MerchandiseReturnShipping {
                ProviderSystemName = Carrier,
                OptionCode = "parcel",
                LabelStatus = labelStatus,
                ProviderData = new Dictionary<string, string> { { "point", "P1" }, { "kept", "yes" } }
            }
        };
    }

    private void SetupLabel(ReturnLabelResult result)
    {
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>())).ReturnsAsync(result);
    }

    private static ReturnLabelResult Created()
    {
        return new ReturnLabelResult {
            Success = true,
            TrackingNumber = "T123",
            ReturnCode = "CODE1",
            Instructions = "Drop it at the point",
            LabelFile = new ReturnLabelFile([1, 2, 3], "application/pdf", "label-r1.pdf"),
            ProviderData = new Dictionary<string, string> { { "point", "P2" }, { "shipment", "S1" } }
        };
    }

    private void VerifyStaffNote(Times times)
    {
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.MerchandiseReturnId == "r1" && !n.DisplayToCustomer && !string.IsNullOrEmpty(n.Note))), times);
    }

    private void VerifyShippingSaved(Times times)
    {
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturnShipping(It.Is<MerchandiseReturn>(r =>
            r.Id == "r1")), times);
    }

    [TestMethod]
    public async Task CreateLabel_Success_SavesDownloadAndCreated()
    {
        SetupLabel(Created());
        Download saved = null;
        _downloadService.Setup(x => x.InsertDownload(It.IsAny<Download>()))
            .Callback<Download>(x =>
            {
                saved = x;
                x.Id = "d1";
            })
            .Returns(Task.CompletedTask);

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.IsNotNull(saved);
        Assert.AreNotEqual(Guid.Empty, saved.DownloadGuid);
        Assert.IsFalse(saved.UseDownloadUrl);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, saved.DownloadBinary);
        Assert.AreEqual("application/pdf", saved.ContentType);
        Assert.AreEqual("label-r1", saved.Filename);
        Assert.AreEqual(".pdf", saved.Extension);
        Assert.AreEqual(DownloadType.MerchandiseReturn, saved.DownloadType);
        Assert.AreEqual("c1", saved.CustomerId);
        Assert.AreEqual("r1", saved.ReferenceId);

        var shipping = _stored.ReturnShipping;
        Assert.AreEqual(ReturnLabelStatus.Created, shipping.LabelStatus);
        Assert.IsNull(shipping.LabelError);
        Assert.AreEqual("d1", shipping.LabelDownloadId);
        Assert.AreEqual("T123", shipping.TrackingNumber);
        Assert.AreEqual("CODE1", shipping.ReturnCode);
        Assert.AreEqual("Drop it at the point", shipping.Instructions);
        //the provider's keys overwrite, the others are kept
        Assert.AreEqual("P2", shipping.ProviderData["point"]);
        Assert.AreEqual("S1", shipping.ProviderData["shipment"]);
        Assert.AreEqual("yes", shipping.ProviderData["kept"]);
        VerifyShippingSaved(Times.Once());
        VerifyStaffNote(Times.Never());
    }

    [TestMethod]
    public async Task CreateLabel_SavesOnlyTheReturnShipping()
    {
        SetupLabel(Created());

        await _service.CreateLabel("r1");

        //the provider was called outside the order's lock: a whole return saved now would undo a status changed
        //in the meantime
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
        VerifyShippingSaved(Times.Once());
    }

    [TestMethod]
    public async Task CreateLabel_NoFile_CreatedWithoutDownload()
    {
        var result = Created();
        result.LabelFile = null;
        SetupLabel(result);

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.Created, _stored.ReturnShipping.LabelStatus);
        Assert.IsNull(_stored.ReturnShipping.LabelDownloadId);
        _downloadService.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateLabel_ProviderThrows_MarksFailedAndKeepsReturn()
    {
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()))
            .ThrowsAsync(new TimeoutException("carrier timed out"));

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.Failed, _stored.ReturnShipping.LabelStatus);
        Assert.AreEqual("carrier timed out", _stored.ReturnShipping.LabelError);
        VerifyShippingSaved(Times.Once());
        VerifyStaffNote(Times.Once());
        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
        _downloadService.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateLabel_ProviderFails_MarksFailedWithItsError()
    {
        SetupLabel(new ReturnLabelResult { Success = false, Error = "address rejected" });

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.Failed, _stored.ReturnShipping.LabelStatus);
        Assert.AreEqual("address rejected", _stored.ReturnShipping.LabelError);
        VerifyShippingSaved(Times.Once());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CreateLabel_ProviderNotInstalled_MarksFailed()
    {
        _returnShippingService.Setup(x => x.LoadProviderBySystemName(Carrier)).Returns((IReturnShippingProvider)null);

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.Failed, _stored.ReturnShipping.LabelStatus);
        Assert.AreEqual("provider not installed", _stored.ReturnShipping.LabelError);
        VerifyShippingSaved(Times.Once());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CreateLabel_AlreadyCreatedUnderLock_DoesNotCallProvider()
    {
        //the page showed the label pending; the first click saved its label while the second waited for the lock
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback(() => _stored = Return(ReturnLabelStatus.Created))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        _provider.Verify(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()), Times.Never);
        VerifyShippingSaved(Times.Never());
    }

    [TestMethod]
    public async Task CreateLabel_RejectedReturn_NotAllowed()
    {
        _stored = Return(ReturnLabelStatus.Failed, MerchandiseReturnStatus.Rejected);
        SetupLabel(Created());

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.LabelNotAllowed, error);
        _provider.Verify(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()), Times.Never);
        VerifyShippingSaved(Times.Never());
    }

    [TestMethod]
    public async Task CreateLabel_CancelledLabel_NotAllowed()
    {
        _stored = Return(ReturnLabelStatus.Cancelled);

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.LabelNotAllowed, error);
        _provider.Verify(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateLabel_NoReturnShipping_NotAllowed()
    {
        _stored.ReturnShipping = null;

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.LabelNotAllowed, error);
    }

    [TestMethod]
    public async Task CreateLabel_UnknownReturn_NotFound()
    {
        _stored = null;

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, error);
    }

    [TestMethod]
    public async Task CreateLabel_LockTimeout_Busy()
    {
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceLockTimeoutException(LabelKey));

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.Busy, error);
        _provider.Verify(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateLabel_Failed_RetrySucceeds()
    {
        _stored = Return(ReturnLabelStatus.Failed);
        _stored.ReturnShipping.LabelError = "carrier timed out";
        SetupLabel(Created());

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.Created, _stored.ReturnShipping.LabelStatus);
        Assert.IsNull(_stored.ReturnShipping.LabelError);
        Assert.AreEqual("d1", _stored.ReturnShipping.LabelDownloadId);
        _provider.Verify(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateLabel_NotRequired_StoresInstructionsOnly()
    {
        _stored = Return(ReturnLabelStatus.NotRequired);
        SetupLabel(Created());

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        var shipping = _stored.ReturnShipping;
        Assert.AreEqual(ReturnLabelStatus.NotRequired, shipping.LabelStatus);
        Assert.AreEqual("Drop it at the point", shipping.Instructions);
        Assert.AreEqual("CODE1", shipping.ReturnCode);
        Assert.IsNull(shipping.TrackingNumber);
        Assert.IsNull(shipping.LabelDownloadId);
        Assert.AreEqual("P1", shipping.ProviderData["point"]);
        _downloadService.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
        VerifyShippingSaved(Times.Once());
    }

    [TestMethod]
    public async Task CreateLabel_NotRequiredWithInstructions_RunsOnce()
    {
        _stored = Return(ReturnLabelStatus.NotRequired);
        _stored.ReturnShipping.Instructions = "Send it to the store";
        SetupLabel(Created());

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual("Send it to the store", _stored.ReturnShipping.Instructions);
        _provider.Verify(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()), Times.Never);
        VerifyShippingSaved(Times.Never());
    }

    [TestMethod]
    public async Task CreateLabel_NotRequiredProviderThrows_StaysNotRequiredWithNote()
    {
        _stored = Return(ReturnLabelStatus.NotRequired);
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()))
            .ThrowsAsync(new FormatException("bad placeholder"));

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.NotRequired, _stored.ReturnShipping.LabelStatus);
        Assert.IsNull(_stored.ReturnShipping.Instructions);
        VerifyShippingSaved(Times.Never());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CreateLabel_TakesLabelLockNotQuantityLock()
    {
        //the provider is called under the label lock only; the result is saved under the order's lock as well
        List<string> heldDuringCall = null;
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()))
            .Callback(() => heldDuringCall = _held.ToList())
            .ReturnsAsync(Created());
        List<string> heldDuringSave = null;
        _merchandiseReturnService.Setup(x => x.UpdateMerchandiseReturnShipping(It.IsAny<MerchandiseReturn>()))
            .Callback(() => heldDuringSave = _held.ToList())
            .Returns(Task.CompletedTask);

        await _service.CreateLabel("r1");

        CollectionAssert.AreEquivalent(new[] { LabelKey }, heldDuringCall);
        CollectionAssert.AreEquivalent(new[] { LabelKey, OrderKey }, heldDuringSave);
        CollectionAssert.AreEqual(new[] { LabelKey, OrderKey }, _acquired);
        Assert.IsEmpty(_held);
    }

    [TestMethod]
    public async Task CreateLabel_OrderLockTimeout_BusyAndNotSaved()
    {
        SetupLabel(Created());
        _resourceLock.Setup(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Contains(OrderKey)),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceLockTimeoutException(OrderKey));

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.Busy, error);
        VerifyShippingSaved(Times.Never());
        _downloadService.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateLabel_ReturnDeletedDuringCall_NoSave()
    {
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()))
            .Callback(() => _stored = null)
            .ReturnsAsync(Created());

        var error = await _service.CreateLabel("r1");

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, error);
        VerifyShippingSaved(Times.Never());
        _downloadService.Verify(x => x.InsertDownload(It.IsAny<Download>()), Times.Never);
        VerifyStaffNote(Times.Never());
    }

    [TestMethod]
    public async Task CreateLabel_ReturnRejectedDuringCall_CancelsNewLabel()
    {
        //staff rejected the return, under the order's lock, while the provider was creating its label
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()))
            .Callback(() => _stored = Return(ReturnLabelStatus.Pending, MerchandiseReturnStatus.Rejected))
            .ReturnsAsync(Created());
        List<string> heldDuringCancel = null;
        _provider.Setup(x => x.CancelLabel(It.IsAny<MerchandiseReturn>()))
            .Callback(() => heldDuringCancel = _held.ToList())
            .ReturnsAsync(true);

        var error = await _service.CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(MerchandiseReturnStatus.Rejected, _stored.MerchandiseReturnStatus);
        Assert.AreEqual(ReturnLabelStatus.Cancelled, _stored.ReturnShipping.LabelStatus);
        Assert.AreEqual("d1", _stored.ReturnShipping.LabelDownloadId);
        _provider.Verify(x => x.CancelLabel(It.Is<MerchandiseReturn>(r => r.Id == "r1")), Times.Once);
        CollectionAssert.AreEquivalent(new[] { LabelKey }, heldDuringCancel);
        //the label lock once, the order's lock for the Created save and again for the Cancelled save
        CollectionAssert.AreEqual(new[] { LabelKey, OrderKey, OrderKey }, _acquired);
        VerifyShippingSaved(Times.Exactly(2));
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateLabel_ProviderTimesOut_MarksFailed()
    {
        _provider.Setup(x => x.CreateLabel(It.IsAny<MerchandiseReturn>()))
            .Returns(new TaskCompletionSource<ReturnLabelResult>().Task);

        var error = await ShortTimeoutService().CreateLabel("r1");

        Assert.IsNull(error);
        Assert.AreEqual(ReturnLabelStatus.Failed, _stored.ReturnShipping.LabelStatus);
        Assert.AreEqual("timed out", _stored.ReturnShipping.LabelError);
        VerifyShippingSaved(Times.Once());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CancelLabel_ProviderTrue_Cancelled()
    {
        _stored = Return(ReturnLabelStatus.Created, MerchandiseReturnStatus.Rejected);
        _provider.Setup(x => x.CancelLabel(It.IsAny<MerchandiseReturn>())).ReturnsAsync(true);

        await _service.CancelLabel("r1");

        Assert.AreEqual(ReturnLabelStatus.Cancelled, _stored.ReturnShipping.LabelStatus);
        Assert.AreEqual(MerchandiseReturnStatus.Rejected, _stored.MerchandiseReturnStatus);
        VerifyShippingSaved(Times.Once());
        VerifyStaffNote(Times.Never());
        CollectionAssert.AreEqual(new[] { LabelKey, OrderKey }, _acquired);
    }

    [TestMethod]
    public async Task CancelLabel_ProviderFalse_StaysCreatedWithNote()
    {
        _stored = Return(ReturnLabelStatus.Created, MerchandiseReturnStatus.Cancelled);
        _provider.Setup(x => x.CancelLabel(It.IsAny<MerchandiseReturn>())).ReturnsAsync(false);

        await _service.CancelLabel("r1");

        Assert.AreEqual(ReturnLabelStatus.Created, _stored.ReturnShipping.LabelStatus);
        VerifyShippingSaved(Times.Never());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CancelLabel_ProviderThrows_StaysCreatedWithNote()
    {
        _stored = Return(ReturnLabelStatus.Created, MerchandiseReturnStatus.Cancelled);
        _provider.Setup(x => x.CancelLabel(It.IsAny<MerchandiseReturn>()))
            .ThrowsAsync(new HttpRequestException("carrier down"));

        await _service.CancelLabel("r1");

        Assert.AreEqual(ReturnLabelStatus.Created, _stored.ReturnShipping.LabelStatus);
        VerifyShippingSaved(Times.Never());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CancelLabel_ProviderTimesOut_StaysCreatedWithNote()
    {
        _stored = Return(ReturnLabelStatus.Created, MerchandiseReturnStatus.Cancelled);
        _provider.Setup(x => x.CancelLabel(It.IsAny<MerchandiseReturn>()))
            .Returns(new TaskCompletionSource<bool>().Task);

        await ShortTimeoutService().CancelLabel("r1");

        Assert.AreEqual(ReturnLabelStatus.Created, _stored.ReturnShipping.LabelStatus);
        VerifyShippingSaved(Times.Never());
        VerifyStaffNote(Times.Once());
    }

    [TestMethod]
    public async Task CancelLabel_NotCreated_DoesNotCallProvider()
    {
        _stored = Return(ReturnLabelStatus.Failed, MerchandiseReturnStatus.Rejected);

        await _service.CancelLabel("r1");

        _provider.Verify(x => x.CancelLabel(It.IsAny<MerchandiseReturn>()), Times.Never);
        VerifyShippingSaved(Times.Never());
    }

    [TestMethod]
    public async Task UpdateShipping_SetsTrackingNumber()
    {
        var error = await _service.UpdateShipping("r1", shipping => shipping.TrackingNumber = "TRACK-9");

        Assert.IsNull(error);
        Assert.AreEqual("TRACK-9", _stored.ReturnShipping.TrackingNumber);
        Assert.AreEqual(MerchandiseReturnStatus.Pending, _stored.MerchandiseReturnStatus);
        VerifyShippingSaved(Times.Once());
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
        CollectionAssert.AreEqual(new[] { LabelKey, OrderKey }, _acquired);
    }

    [TestMethod]
    public async Task UpdateShipping_NotAllowedOnStoredCopy_ChangesNothing()
    {
        //the staff marked the return received after the customer opened the page
        _stored.MerchandiseReturnStatus = MerchandiseReturnStatus.Received;

        var error = await _service.UpdateShipping("r1", shipping => shipping.TrackingNumber = "TRACK-9",
            stored => stored.MerchandiseReturnStatus == MerchandiseReturnStatus.Pending);

        Assert.IsNull(error);
        Assert.IsNull(_stored.ReturnShipping.TrackingNumber);
    }

    [TestMethod]
    public async Task UpdateShipping_NoReturnShipping_NotFound()
    {
        _stored.ReturnShipping = null;

        var error = await _service.UpdateShipping("r1", shipping => shipping.TrackingNumber = "TRACK-9");

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, error);
        VerifyShippingSaved(Times.Never());
    }
}
