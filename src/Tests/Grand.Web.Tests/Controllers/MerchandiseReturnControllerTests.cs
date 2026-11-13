using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class MerchandiseReturnControllerTests
{
    private readonly Customer _buyer = new() { Id = "buyer" };
    private MerchandiseReturnController _controller;
    private Mock<IDownloadService> _downloads;
    private Mock<IMerchandiseReturnLabelService> _labels;
    private Mock<IMediator> _mediator;
    private Mock<IOrderService> _orders;
    private Mock<IMerchandiseReturnService> _returns;
    private Mock<IRequestTranslationService> _translation;
    private Mock<IMerchandiseReturnWorkflowService> _workflow;
    private readonly OrderSettings _orderSettings = new() { MerchandiseReturnsEnabled = true };

    [TestInitialize]
    public void Init()
    {
        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(_buyer);
        work.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "l1" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        context.Setup(x => x.StoreContext).Returns(storeContext.Object);

        _orders = new Mock<IOrderService>();
        _orders.Setup(x => x.GetOrderById("o1"))
            .ReturnsAsync(new Order { Id = "o1", CustomerId = "buyer", StoreId = "s1" });
        _returns = new Mock<IMerchandiseReturnService>();
        _labels = new Mock<IMerchandiseReturnLabelService>();
        _workflow = new Mock<IMerchandiseReturnWorkflowService>();
        _downloads = new Mock<IDownloadService>();
        _mediator = new Mock<IMediator>();
        var translation = _translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns("text");
        //a resource is found for the key, a provider's text is not: it comes back as the default value
        translation.Setup(x => x.GetResource(It.IsAny<string>(), "l1", It.IsAny<string>(), It.IsAny<bool>()))
            .Returns<string, string, string, bool>((name, _, defaultValue, _) =>
                name == "MerchandiseReturns.ReturnShipping.Choose" ? "Choose a method." : defaultValue);

        //a customer without sub-accounts counts as an owner, as GroupService answers it
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.IsOwner(_buyer)).ReturnsAsync(true);

        _controller = new MerchandiseReturnController(_returns.Object, new Mock<IPickupPointService>().Object,
            _orders.Object, context.Object, groupService.Object, translation.Object, _mediator.Object,
            _labels.Object, _downloads.Object, new AddressSettings(), _orderSettings, _workflow.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private MerchandiseReturn Return(string customerId, string providerSystemName = null,
        MerchandiseReturnStatus status = MerchandiseReturnStatus.Pending)
    {
        var merchandiseReturn = new MerchandiseReturn {
            Id = "r1", OrderId = "o1", CustomerId = customerId, MerchandiseReturnStatus = status,
            ReturnShipping = new MerchandiseReturnShipping {
                ProviderSystemName = providerSystemName ?? MerchandiseReturnShippingExtensions.SendYourselfSystemName,
                LabelDownloadId = "d1",
                LabelStatus = ReturnLabelStatus.Created
            }
        };
        _returns.Setup(x => x.GetMerchandiseReturnById("r1")).ReturnsAsync(merchandiseReturn);
        return merchandiseReturn;
    }

    [TestMethod]
    public async Task MerchandiseReturn_ShippingErrors_KeyTranslatedProviderTextAsItIs()
    {
        //Arrange - a key of the return service and a text the provider translated already
        var submitResult = new MerchandiseReturnSubmitResult();
        submitResult.ShippingErrors.Add("MerchandiseReturns.ReturnShipping.Choose");
        submitResult.ShippingErrors.Add("Pick a Parcel Locker near you.");
        _mediator.Setup(x => x.Send(It.IsAny<MerchandiseReturnSubmitCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submitResult);
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MerchandiseReturnModel());

        //Act
        var result = await _controller.MerchandiseReturn(new MerchandiseReturnModel { OrderId = "o1" });

        //Assert
        var model = (MerchandiseReturnModel)((ViewResult)result).Model;
        Assert.AreEqual("Choose a method., Pick a Parcel Locker near you.", model.Error);
    }

    [TestMethod]
    public async Task MerchandiseReturnLabel_OtherCustomer_NotFound()
    {
        Return("someone-else");

        var result = await _controller.MerchandiseReturnLabel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _downloads.Verify(x => x.GetDownloadById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task MerchandiseReturnLabel_Owner_ReturnsFile()
    {
        Return("buyer");
        var content = new byte[] { 1, 2, 3 };
        _downloads.Setup(x => x.GetDownloadById("d1")).ReturnsAsync(new Download {
            Id = "d1", DownloadBinary = content, ContentType = "application/pdf", Filename = "label-1",
            Extension = ".pdf"
        });

        var result = await _controller.MerchandiseReturnLabel("r1");

        var file = (FileContentResult)result;
        CollectionAssert.AreEqual(content, file.FileContents);
        Assert.AreEqual("application/pdf", file.ContentType);
        Assert.AreEqual("label-1.pdf", file.FileDownloadName);
    }

    [TestMethod]
    public async Task MerchandiseReturnLabel_NoContentType_SentAsOctetStream()
    {
        Return("buyer");
        _downloads.Setup(x => x.GetDownloadById("d1")).ReturnsAsync(new Download {
            Id = "d1", DownloadBinary = [1], ContentType = null, Filename = "label-1", Extension = ".pdf"
        });

        var result = await _controller.MerchandiseReturnLabel("r1");

        Assert.AreEqual("application/octet-stream", ((FileContentResult)result).ContentType);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    public async Task MerchandiseReturnLabel_ClosedReturnWithLabelStillCreated_NotFound(
        MerchandiseReturnStatus status)
    {
        //the carrier did not cancel the label: it stays created, but the parcel is not to be sent any more
        Return("buyer", status: status);

        var result = await _controller.MerchandiseReturnLabel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _downloads.Verify(x => x.GetDownloadById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task MerchandiseReturnLabel_NoLabel_NotFound()
    {
        Return("buyer").ReturnShipping.LabelDownloadId = null;

        var result = await _controller.MerchandiseReturnLabel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task MerchandiseReturnTrackingNumber_NotSendYourself_NotFound()
    {
        Return("buyer", "ReturnShipping.InStore");

        var result = await _controller.MerchandiseReturnTrackingNumber("r1", "TN1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _labels.Verify(x => x.UpdateShipping(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturnShipping>>(),
            It.IsAny<Func<MerchandiseReturn, bool>>()), Times.Never);
    }

    [TestMethod]
    public async Task MerchandiseReturnTrackingNumber_Received_NotFound()
    {
        //the parcel arrived already: the number is of no use any more
        Return("buyer", status: MerchandiseReturnStatus.Received);

        var result = await _controller.MerchandiseReturnTrackingNumber("r1", "TN1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task MerchandiseReturnTrackingNumber_SendYourself_SavesTheNumber()
    {
        Return("buyer");
        var shipping = new MerchandiseReturnShipping();
        Func<MerchandiseReturn, bool> allowed = null;
        _labels.Setup(x => x.UpdateShipping("r1", It.IsAny<Action<MerchandiseReturnShipping>>(),
                It.IsAny<Func<MerchandiseReturn, bool>>()))
            .Callback<string, Action<MerchandiseReturnShipping>, Func<MerchandiseReturn, bool>>((_, apply, check) =>
            {
                apply(shipping);
                allowed = check;
            })
            .ReturnsAsync((string)null);

        var result = await _controller.MerchandiseReturnTrackingNumber("r1", " TN1 ");

        Assert.AreEqual("MerchandiseReturnDetails", ((RedirectToRouteResult)result).RouteName);
        Assert.AreEqual("TN1", shipping.TrackingNumber);
        //checked again on the return as stored: a return received in the meantime takes no number
        Assert.IsTrue(allowed(Return("buyer")));
        Assert.IsFalse(allowed(Return("buyer", status: MerchandiseReturnStatus.Received)));
    }

    [TestMethod]
    public async Task MerchandiseReturnRetryLabel_OtherCustomer_NotFound()
    {
        Return("someone-else").ReturnShipping.LabelStatus = ReturnLabelStatus.Failed;

        var result = await _controller.MerchandiseReturnRetryLabel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _labels.Verify(x => x.CreateLabel(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task MerchandiseReturnRetryLabel_Failed_CreatesTheLabel()
    {
        Return("buyer").ReturnShipping.LabelStatus = ReturnLabelStatus.Failed;
        _labels.Setup(x => x.CreateLabel("r1")).ReturnsAsync((string)null);

        var result = await _controller.MerchandiseReturnRetryLabel("r1");

        Assert.AreEqual("MerchandiseReturnDetails", ((RedirectToRouteResult)result).RouteName);
        _labels.Verify(x => x.CreateLabel("r1"), Times.Once);
    }

    [TestMethod]
    public async Task MerchandiseReturnRetryLabel_Created_DoesNotCallTheProvider()
    {
        //the label is there already: nothing to retry
        Return("buyer");

        var result = await _controller.MerchandiseReturnRetryLabel("r1");

        Assert.AreEqual("MerchandiseReturnDetails", ((RedirectToRouteResult)result).RouteName);
        _labels.Verify(x => x.CreateLabel(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task MerchandiseReturn_PostedPhotos_PassedByOrderItem()
    {
        var files = new FormFileCollection {
            new FormFile(new MemoryStream([1]), 0, 1, "photos.a", "front.png"),
            new FormFile(new MemoryStream([1]), 0, 1, "photos.a", "back.png"),
            new FormFile(new MemoryStream([1]), 0, 1, "other", "x.png")
        };
        _controller.ControllerContext.HttpContext.Request.ContentType = "multipart/form-data; boundary=b";
        _controller.ControllerContext.HttpContext.Request.Form = new FormCollection([], files);
        MerchandiseReturnSubmitCommand sent = null;
        _mediator.Setup(x => x.Send(It.IsAny<MerchandiseReturnSubmitCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MerchandiseReturnSubmitResult>, CancellationToken>((c, _) =>
                sent = (MerchandiseReturnSubmitCommand)c)
            .ReturnsAsync(new MerchandiseReturnSubmitResult());

        await _controller.MerchandiseReturn(new MerchandiseReturnModel { OrderId = "o1" });

        Assert.AreEqual(1, sent.Photos.Count);
        CollectionAssert.AreEqual(new[] { "front.png", "back.png" },
            sent.Photos["a"].Select(x => x.FileName).ToArray());
    }

    [TestMethod]
    public async Task Cancel_Pending_ChangesStatusWithPendingOnly()
    {
        Return("buyer");
        var pending = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Pending };
        var accepted = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted };

        var result = await _controller.MerchandiseReturnCancel("r1");

        Assert.AreEqual("MerchandiseReturnDetails", ((RedirectToRouteResult)result).RouteName);
        //checked by the workflow on the return as stored: the staff may have accepted it since the page was loaded
        _workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Cancelled, null, "customer buyer",
            It.Is<Func<MerchandiseReturn, bool>>(allowed => allowed(pending) && !allowed(accepted))), Times.Once);
    }

    [TestMethod]
    public async Task Cancel_Refused_ErrorShown()
    {
        Return("buyer");
        _translation.Setup(x => x.GetResource("MerchandiseReturns.Cancel.NotAllowed")).Returns("Too late.");
        _workflow.Setup(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Cancelled, null, It.IsAny<string>(),
                It.IsAny<Func<MerchandiseReturn, bool>>()))
            .ReturnsAsync(MerchandiseReturnErrors.StatusChangeNotAllowed);

        var result = await _controller.MerchandiseReturnCancel("r1");

        Assert.AreEqual("MerchandiseReturnDetails", ((RedirectToRouteResult)result).RouteName);
        Assert.IsTrue(((IList<string>)_controller.TempData["grand.notifications.Error"]).Contains("Too late."));
    }

    [TestMethod]
    public async Task Cancel_OtherStore_NotFound()
    {
        Return("buyer");
        OrderOfOtherStore();

        var result = await _controller.MerchandiseReturnCancel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<MerchandiseReturn, bool>>()), Times.Never);
    }

    [TestMethod]
    public async Task Cancel_OtherCustomer_NotFound()
    {
        Return("other");

        var result = await _controller.MerchandiseReturnCancel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task AddNote_OtherStore_NotFound()
    {
        Return("buyer");
        OrderOfOtherStore();

        var result = await _controller.MerchandiseReturnAddNote("r1", "Hello", null);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _mediator.Verify(x => x.Send(It.IsAny<InsertMerchandiseReturnNoteCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddNote_Added_RedirectsWithNotification()
    {
        Return("buyer");
        _translation.Setup(x => x.GetResource("MerchandiseReturns.Notes.Added")).Returns("Sent.");
        _mediator.Setup(x => x.Send(It.IsAny<InsertMerchandiseReturnNoteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var result = await _controller.MerchandiseReturnAddNote("r1", "Hello", null);

        Assert.AreEqual("MerchandiseReturnDetails", ((RedirectToRouteResult)result).RouteName);
        Assert.IsTrue(((IList<string>)_controller.TempData["grand.notifications.Success"]).Contains("Sent."));
        _mediator.Verify(x => x.Send(It.Is<InsertMerchandiseReturnNoteCommand>(c =>
                c.MerchandiseReturn.Id == "r1" && c.Order.Id == "o1" && c.Note == "Hello" && c.Customer == _buyer),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Pdf_OtherStore_NotFound()
    {
        Return("buyer");
        OrderOfOtherStore();
        var pdfService = new Mock<IPdfService>();

        var result = await _controller.MerchandiseReturnPdf("r1", pdfService.Object);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        pdfService.Verify(x => x.PrintMerchandiseReturnToPdf(It.IsAny<Stream>(), It.IsAny<MerchandiseReturn>(),
            It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Pdf_Owner_Pdf()
    {
        var merchandiseReturn = Return("buyer");
        merchandiseReturn.ReturnNumber = 31;
        var pdfService = new Mock<IPdfService>();
        pdfService.Setup(x => x.PrintMerchandiseReturnToPdf(It.IsAny<Stream>(), merchandiseReturn,
                It.Is<Order>(o => o.Id == "o1"), "l1", It.IsAny<CancellationToken>()))
            .Callback<Stream, MerchandiseReturn, Order, string, CancellationToken>((s, _, _, _, _) => s.WriteByte(1))
            .Returns(Task.CompletedTask);

        var result = await _controller.MerchandiseReturnPdf("r1", pdfService.Object);

        var file = (FileContentResult)result;
        Assert.AreEqual("application/pdf", file.ContentType);
        Assert.AreEqual("return_31.pdf", file.FileDownloadName);
        Assert.AreEqual(1, file.FileContents.Length);
    }

    [TestMethod]
    public async Task Estimate_OtherStore_NotFound()
    {
        OrderOfOtherStore();

        var result = await _controller.MerchandiseReturnEstimate("o1", new MerchandiseReturnModel());

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _mediator.Verify(x => x.Send(It.IsAny<GetMerchandiseReturnEstimate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Estimate_ReturnsDisabled_Empty()
    {
        _orderSettings.MerchandiseReturnsEnabled = false;

        var result = await _controller.MerchandiseReturnEstimate("o1", new MerchandiseReturnModel());

        var estimate = (MerchandiseReturnEstimateModel)((JsonResult)result).Value;
        Assert.IsNull(estimate.Amount);
        _mediator.Verify(x => x.Send(It.IsAny<GetMerchandiseReturnEstimate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     The order of the return was placed in another store than the one the customer is in
    /// </summary>
    private void OrderOfOtherStore()
    {
        _orders.Setup(x => x.GetOrderById("o1"))
            .ReturnsAsync(new Order { Id = "o1", CustomerId = "buyer", StoreId = "s2" });
    }

    [TestMethod]
    public async Task Details_OtherStore_Challenge()
    {
        Return("buyer");
        OrderOfOtherStore();

        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
        _mediator.Verify(x => x.Send(It.IsAny<GetMerchandiseReturnDetails>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task MerchandiseReturnLabel_OtherStore_NotFound()
    {
        Return("buyer");
        OrderOfOtherStore();
        _downloads.Setup(x => x.GetDownloadById("d1"))
            .ReturnsAsync(new Download { Id = "d1", DownloadBinary = [1], Filename = "label", Extension = ".pdf" });

        var result = await _controller.MerchandiseReturnLabel("r1");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }
}
