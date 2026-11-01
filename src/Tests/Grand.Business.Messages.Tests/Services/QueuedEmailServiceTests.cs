using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Messages.Services;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Messages;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class QueuedEmailServiceTests
{
    private Mock<IDownloadService> _downloadServiceMock;
    private Mock<ILogger<QueuedEmailService>> _loggerMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<QueuedEmail>> _repository;
    private QueuedEmailService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new Mock<IRepository<QueuedEmail>>();
        _repository.Setup(c => c.ToListAsync(It.IsAny<IQueryable<QueuedEmail>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<QueuedEmail> query, CancellationToken _) =>
                Task.FromResult<IList<QueuedEmail>>(query.ToList()));
        _downloadServiceMock = new Mock<IDownloadService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<QueuedEmailService>>();
        _service = new QueuedEmailService(_repository.Object, _downloadServiceMock.Object, _mediatorMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task GetQueuedEmailByIdTest()
    {
        await _service.GetQueuedEmailById("1");
        _repository.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task InsertQueuedEmail_ValidArgument_InvokeExpectedMethods()
    {
        await _service.InsertQueuedEmail(new QueuedEmail());
        _repository.Verify(c => c.InsertAsync(It.IsAny<QueuedEmail>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<QueuedEmail>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task InsertQueuedEmail_NotStored_DeletesTheInvoiceAndRethrows()
    {
        _repository.Setup(c => c.InsertAsync(It.IsAny<QueuedEmail>())).ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            _service.InsertQueuedEmail(new QueuedEmail { InvoiceDownloadId = "invoice" }));

        _downloadServiceMock.Verify(c => c.DeleteDownloadById("invoice"), Times.Once);
    }

    [TestMethod]
    public async Task InsertQueuedEmail_StoredButEventFails_KeepsTheInvoice()
    {
        _mediatorMock.Setup(c => c.Publish(It.IsAny<EntityInserted<QueuedEmail>>(), default))
            .ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            _service.InsertQueuedEmail(new QueuedEmail { InvoiceDownloadId = "invoice" }));

        _downloadServiceMock.Verify(c => c.DeleteDownloadById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void InsertQueuedEmail_NullArguemnt_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await _service.InsertQueuedEmail(new QueuedEmail()));
    }

    [TestMethod]
    public async Task UpdateQueuedEmail_ValidArgument_InvokeExpectedMethods()
    {
        await _service.UpdateQueuedEmail(new QueuedEmail());
        _repository.Verify(c => c.UpdateAsync(It.IsAny<QueuedEmail>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<QueuedEmail>>(), default), Times.Once);
    }

    [TestMethod]
    public void UpdateQueuedEmai_NullArguemnt_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await _service.UpdateQueuedEmail(new QueuedEmail()));
    }

    [TestMethod]
    public async Task DeleteQueuedEmail_ValidArgument_InvokeExpectedMethods()
    {
        await _service.DeleteQueuedEmail(new QueuedEmail());
        _repository.Verify(c => c.DeleteAsync(It.IsAny<QueuedEmail>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<QueuedEmail>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCustomerEmail_ValidArgument_InvokeExpectedMethods()
    {
        await _service.DeleteCustomerEmail("email@email.com");
        _repository.Verify(c => c.DeleteManyAsync(It.IsAny<Expression<Func<QueuedEmail, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCustomerEmail_AnEmailWithAnInvoiceIsLeftToTheInvoiceDeletion()
    {
        Expression<Func<QueuedEmail, bool>> filter = null;
        _repository.Setup(c => c.DeleteManyAsync(It.IsAny<Expression<Func<QueuedEmail, bool>>>()))
            .Callback<Expression<Func<QueuedEmail, bool>>>(f => filter = f);

        await _service.DeleteCustomerEmail("a@b.c");

        var matches = filter.Compile();
        Assert.IsTrue(matches(new QueuedEmail { To = "a@b.c" }));
        Assert.IsFalse(matches(new QueuedEmail { To = "other@b.c" }));
        //queued after the invoices were read, so its invoice was not deleted
        Assert.IsFalse(matches(new QueuedEmail { To = "a@b.c", InvoiceDownloadId = "queued-meanwhile" }));
    }

    [TestMethod]
    public async Task DeleteAllEmails_ValidArgument_InvokeExpectedMethods()
    {
        await _service.DeleteAllEmails();
        _repository.Verify(c => c.DeleteManyAsync(It.IsAny<Expression<Func<QueuedEmail, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAllEmails_AnEmailWithAnInvoiceIsLeftToTheInvoiceDeletion()
    {
        Expression<Func<QueuedEmail, bool>> filter = null;
        _repository.Setup(c => c.DeleteManyAsync(It.IsAny<Expression<Func<QueuedEmail, bool>>>()))
            .Callback<Expression<Func<QueuedEmail, bool>>>(f => filter = f);

        await _service.DeleteAllEmails();

        var matches = filter.Compile();
        Assert.IsTrue(matches(new QueuedEmail()));
        //queued after the invoices were read, so its invoice was not deleted
        Assert.IsFalse(matches(new QueuedEmail { InvoiceDownloadId = "queued-meanwhile" }));
    }

    [TestMethod]
    public void DeleteQueuedEmai_NullArguemnt_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await _service.DeleteQueuedEmail(new QueuedEmail()));
    }

    [TestMethod]
    public async Task DeleteQueuedEmail_DeletesTheInvoiceButNotTheSharedAttachments()
    {
        await _service.DeleteQueuedEmail(new QueuedEmail {
            InvoiceDownloadId = "invoice", AttachedDownloads = ["template-attachment"]
        });

        _downloadServiceMock.Verify(c => c.DeleteDownloadById("invoice"), Times.Once);
        _downloadServiceMock.Verify(c => c.DeleteDownloadById("template-attachment"), Times.Never);
    }

    [TestMethod]
    public async Task DeleteQueuedEmail_InvoiceCannotBeDeleted_LogsAndStillPublishesTheEvent()
    {
        _downloadServiceMock.Setup(c => c.DeleteDownloadById("invoice")).ThrowsAsync(new InvalidOperationException());

        await _service.DeleteQueuedEmail(new QueuedEmail { InvoiceDownloadId = "invoice" });

        _repository.Verify(c => c.DeleteAsync(It.IsAny<QueuedEmail>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<QueuedEmail>>(), default), Times.Once);
        VerifyErrorLogged(Times.Once());
    }

    [TestMethod]
    public async Task DeleteCustomerEmail_DeletesOnlyTheInvoicesOfThatCustomer()
    {
        SetupEmails(
            new QueuedEmail { To = "a@b.c", InvoiceDownloadId = "mine" },
            new QueuedEmail { To = "a@b.c" },
            new QueuedEmail { To = "other@b.c", InvoiceDownloadId = "other" });

        await _service.DeleteCustomerEmail("a@b.c");

        _downloadServiceMock.Verify(c => c.DeleteDownloadById("mine"), Times.Once);
        _downloadServiceMock.Verify(c => c.DeleteDownloadById(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCustomerEmail_InvoiceCannotBeDeleted_DoesNotStopTheRest()
    {
        SetupEmails(
            new QueuedEmail { To = "a@b.c", InvoiceDownloadId = "broken" },
            new QueuedEmail { To = "a@b.c", InvoiceDownloadId = "fine" });
        _downloadServiceMock.Setup(c => c.DeleteDownloadById("broken")).ThrowsAsync(new InvalidOperationException());

        await _service.DeleteCustomerEmail("a@b.c");

        _downloadServiceMock.Verify(c => c.DeleteDownloadById("fine"), Times.Once);
        VerifyErrorLogged(Times.Once());
    }

    [TestMethod]
    public async Task DeleteAllEmails_DeletesEveryInvoice()
    {
        SetupEmails(
            new QueuedEmail { InvoiceDownloadId = "first", AttachedDownloads = ["template-attachment"] },
            new QueuedEmail { InvoiceDownloadId = "second" },
            new QueuedEmail());

        await _service.DeleteAllEmails();

        _downloadServiceMock.Verify(c => c.DeleteDownloadById("first"), Times.Once);
        _downloadServiceMock.Verify(c => c.DeleteDownloadById("second"), Times.Once);
        _downloadServiceMock.Verify(c => c.DeleteDownloadById(It.IsAny<string>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task DeleteAllEmails_DeletesTheInvoicesBeforeTheEmails()
    {
        SetupEmails(new QueuedEmail { InvoiceDownloadId = "invoice" });
        var calls = new List<string>();
        _downloadServiceMock.Setup(c => c.DeleteDownloadById("invoice")).Callback(() => calls.Add("invoice"));
        _repository.Setup(c => c.DeleteAsync(It.IsAny<IEnumerable<QueuedEmail>>()))
            .Callback(() => calls.Add("emails"));

        await _service.DeleteAllEmails();

        CollectionAssert.AreEqual(new[] { "invoice", "emails" }, calls);
    }

    [TestMethod]
    public async Task DeleteOrderInvoiceEmails_DeletesOnlyTheInvoiceEmailsOfThoseOrders()
    {
        var invoiceEmail = new QueuedEmail {
            To = "billing@b.c", Reference = Reference.Order, ObjectId = "order", InvoiceDownloadId = "mine"
        };
        SetupEmails(
            invoiceEmail,
            new QueuedEmail { Reference = Reference.Order, ObjectId = "order" },
            new QueuedEmail { Reference = Reference.Order, ObjectId = "other", InvoiceDownloadId = "other" });
        IEnumerable<QueuedEmail> deleted = null;
        _repository.Setup(c => c.DeleteAsync(It.IsAny<IEnumerable<QueuedEmail>>()))
            .Callback<IEnumerable<QueuedEmail>>(emails => deleted = emails.ToList());

        await _service.DeleteOrderInvoiceEmails(["order"]);

        _downloadServiceMock.Verify(c => c.DeleteDownloadById("mine"), Times.Once);
        _downloadServiceMock.Verify(c => c.DeleteDownloadById(It.IsAny<string>()), Times.Once);
        CollectionAssert.AreEqual(new[] { invoiceEmail.Id }, deleted.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task DeleteOrderInvoiceEmails_NoOrders_DeletesNothing()
    {
        await _service.DeleteOrderInvoiceEmails([]);

        _repository.Verify(c => c.DeleteAsync(It.IsAny<IEnumerable<QueuedEmail>>()), Times.Never);
    }

    [TestMethod]
    public async Task CopyInvoiceDownload_CreatesASeparateDownload()
    {
        var binary = new byte[] { 1, 2, 3 };
        var invoice = new Download {
            Filename = "order_1", Extension = ".pdf", ContentType = "application/pdf", DownloadBinary = binary
        };
        _downloadServiceMock.Setup(c => c.GetDownloadById(invoice.Id)).ReturnsAsync(invoice);
        Download copy = null;
        _downloadServiceMock.Setup(c => c.InsertDownload(It.IsAny<Download>())).Callback<Download>(d => copy = d);

        var result = await _service.CopyInvoiceDownload(invoice.Id);

        Assert.IsNotNull(copy);
        Assert.AreEqual(copy.Id, result);
        Assert.AreNotEqual(invoice.Id, result);
        Assert.AreEqual("order_1", copy.Filename);
        Assert.AreEqual(".pdf", copy.Extension);
        Assert.AreEqual("application/pdf", copy.ContentType);
        Assert.AreSame(binary, copy.DownloadBinary);
        //no guid, so the storefront download action cannot serve it
        Assert.AreEqual(Guid.Empty, copy.DownloadGuid);
    }

    [TestMethod]
    public async Task CopyInvoiceDownload_NoInvoice_ReturnsNull()
    {
        var result = await _service.CopyInvoiceDownload(null);

        Assert.IsNull(result);
        _downloadServiceMock.Verify(c => c.InsertDownload(It.IsAny<Download>()), Times.Never);
    }

    private void SetupEmails(params QueuedEmail[] emails)
    {
        _repository.Setup(c => c.Table).Returns(emails.AsQueryable());
    }

    private void VerifyErrorLogged(Times times)
    {
        _loggerMock.Verify(c => c.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), times);
    }
}