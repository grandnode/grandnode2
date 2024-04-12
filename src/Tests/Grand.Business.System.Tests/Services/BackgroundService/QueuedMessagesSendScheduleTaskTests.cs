using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Domain;
using Grand.Domain.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.System.Tests.Services.BackgroundService;

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

        await _task.Execute();
        _emailSenderMock.Verify(c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            , It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Once);

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
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ThrowsAsync(new Exception());

        await _task.Execute();
        _loggerMock.Verify(c => c.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        _queuedEmailServiceMock.Verify(c => c.UpdateQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once);
    }
}