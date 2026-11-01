using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class QueuedMessagesSendScheduleTaskTests
{
    private Mock<IEmailAccountService> _emailAccountServiceMock;
    private Mock<IEmailSender> _emailSenderMock;
    private Mock<ILogger<QueuedMessagesSendScheduleTask>> _loggerMock;
    private Mock<IQueuedEmailService> _queuedEmailServiceMock;
    private QueuedMessagesSendScheduleTask _task;

    [TestInitialize]
    public void Init()
    {
        _queuedEmailServiceMock = new Mock<IQueuedEmailService>();
        _emailSenderMock = new Mock<IEmailSender>();
        _emailAccountServiceMock = new Mock<IEmailAccountService>();
        _loggerMock = new Mock<ILogger<QueuedMessagesSendScheduleTask>>();
        _task = new QueuedMessagesSendScheduleTask(_queuedEmailServiceMock.Object, _emailSenderMock.Object,
            _loggerMock.Object, _emailAccountServiceMock.Object);
    }

    [TestMethod]
    public async Task Execute_InovekExpectedMethods()
    {
        _queuedEmailServiceMock.Setup(c => c.SearchEmails(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(() => new PagedList<QueuedEmail>
            { new() { Bcc = "bcc", CC = "cc", EmailAccountId = "id" } });
        _emailAccountServiceMock.Setup(c => c.GetEmailAccountById(It.IsAny<string>())).ReturnsAsync(new EmailAccount());

        await _task.Execute(CancellationToken.None);
        _emailSenderMock.Verify(c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            , It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);

        _queuedEmailServiceMock.Verify(c => c.UpdateQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once);
    }

    [TestMethod]
    public async Task Execute_ThrowExecption_InovekExpectedMethodsAndInsertLog()
    {
        _queuedEmailServiceMock.Setup(c => c.SearchEmails(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(() => new PagedList<QueuedEmail>
            { new() { Bcc = "bcc", CC = "cc", EmailAccountId = "id" } });
        _emailAccountServiceMock.Setup(c => c.GetEmailAccountById(It.IsAny<string>())).ReturnsAsync(new EmailAccount());
        _emailSenderMock.Setup(c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            , It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await _task.Execute(CancellationToken.None);
        _loggerMock.Verify(c => c.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        _queuedEmailServiceMock.Verify(c => c.UpdateQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once);
    }

    [TestMethod]
    public async Task Execute_CancelledAfterTheFirstMail_LeavesTheRestQueuedWithUnchangedTries()
    {
        var first = new QueuedEmail { Id = "1", EmailAccountId = "id" };
        var second = new QueuedEmail { Id = "2", EmailAccountId = "id", SentTries = 1 };
        var third = new QueuedEmail { Id = "3", EmailAccountId = "id" };
        SetupQueue(first, second, third);
        using var cts = new CancellationTokenSource();
        _emailSenderMock.Setup(c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .Returns(Task.CompletedTask);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => _task.Execute(cts.Token));

        //the mail sent before the cancellation is recorded as sent
        Assert.IsNotNull(first.SentOnUtc);
        Assert.AreEqual(1, first.SentTries);
        _queuedEmailServiceMock.Verify(c => c.UpdateQueuedEmail(first), Times.Once);
        //the rest stay queued for the next run, without a used try
        Assert.IsNull(second.SentOnUtc);
        Assert.AreEqual(1, second.SentTries);
        Assert.IsNull(third.SentOnUtc);
        Assert.AreEqual(0, third.SentTries);
        _queuedEmailServiceMock.Verify(c => c.UpdateQueuedEmail(It.Is<QueuedEmail>(e => e != first)), Times.Never);
    }

    [TestMethod]
    public async Task Execute_CancelledMidSend_DoesNotCountTheInterruptedTry()
    {
        var queuedEmail = new QueuedEmail { Id = "1", EmailAccountId = "id", SentTries = 2 };
        SetupQueue(queuedEmail);
        using var cts = new CancellationTokenSource();
        _emailSenderMock.Setup(c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(() => {
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() => _task.Execute(cts.Token));

        //the third try would have been the last one - an interrupted send must not use it up
        Assert.AreEqual(2, queuedEmail.SentTries);
        Assert.IsNull(queuedEmail.SentOnUtc);
        _queuedEmailServiceMock.Verify(c => c.UpdateQueuedEmail(It.IsAny<QueuedEmail>()), Times.Never);
    }

    [TestMethod]
    public async Task Execute_EmailWithInvoice_SendsItWithTheOtherAttachments()
    {
        var attachments = await SentAttachments(new QueuedEmail {
            EmailAccountId = "id", InvoiceDownloadId = "invoice", AttachedDownloads = ["template-attachment"]
        });

        CollectionAssert.AreEqual(new[] { "template-attachment", "invoice" }, attachments);
    }

    [TestMethod]
    public async Task Execute_EmailWithoutInvoice_SendsOnlyTheOtherAttachments()
    {
        var attachments = await SentAttachments(new QueuedEmail {
            EmailAccountId = "id", AttachedDownloads = ["template-attachment"]
        });

        CollectionAssert.AreEqual(new[] { "template-attachment" }, attachments);
    }

    private async Task<string[]> SentAttachments(QueuedEmail queuedEmail)
    {
        SetupQueue(queuedEmail);
        string[] attachments = null;
        _emailSenderMock.Setup(c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback((EmailAccount _, string _, string _, string _, string _, string _, string _, string _, string _,
                IEnumerable<string> _, IEnumerable<string> _, string _, string _, IEnumerable<string> downloads,
                CancellationToken _) => attachments = downloads.ToArray())
            .Returns(Task.CompletedTask);

        await _task.Execute(CancellationToken.None);

        return attachments;
    }

    private void SetupQueue(params QueuedEmail[] queuedEmails)
    {
        _queuedEmailServiceMock.Setup(c => c.SearchEmails(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(() => new PagedList<QueuedEmail>(queuedEmails, 0, 500));
        _emailAccountServiceMock.Setup(c => c.GetEmailAccountById(It.IsAny<string>())).ReturnsAsync(new EmailAccount());
    }
}