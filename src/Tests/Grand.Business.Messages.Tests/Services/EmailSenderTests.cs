using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Messages.Services;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net.Sockets;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class EmailSenderTests
{
    private Mock<IDownloadService> _downloadService;
    private Mock<IMimeMappingService> _mimeMappingService;
    private EmailSender _sender;

    [TestInitialize]
    public void Init()
    {
        _downloadService = new Mock<IDownloadService>();
        _mimeMappingService = new Mock<IMimeMappingService>();

        _sender = new EmailSender(_downloadService.Object, _mimeMappingService.Object);
    }

    [TestMethod]
    public void SendEmailTest()
    {
        Assert.ThrowsExceptionAsync<SocketException>(async () =>
            await _sender.SendEmail(new EmailAccount { Host = "admin@admin.com" }, "subject", "body", "admin@store.com",
                "admin store", "customer@email.com", "Customer name"));
    }
}