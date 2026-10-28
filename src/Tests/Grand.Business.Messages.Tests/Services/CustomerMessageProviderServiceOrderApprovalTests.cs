using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Services;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class CustomerMessageProviderServiceOrderApprovalTests
{
    private readonly OrderApproval _approval = new() { Id = "approval1", CustomerId = "buyer1" };
    private readonly Customer _approver = new() { Id = "owner1", Email = "approver@example.com" };
    private readonly Customer _buyer = new() { Id = "buyer1", Email = "buyer@example.com" };
    private readonly Store _store = new() { Id = "store1", Url = "https://store.example/" };
    private Mock<IMediator> _mediatorMock;
    private Mock<IMessageTemplateService> _messageTemplateServiceMock;
    private QueuedEmail _queued;
    private Mock<IQueuedEmailService> _queuedEmailServiceMock;
    private CustomerMessageProviderService _service;

    [TestInitialize]
    public void Init()
    {
        _messageTemplateServiceMock = new Mock<IMessageTemplateService>();
        _queuedEmailServiceMock = new Mock<IQueuedEmailService>();
        _queuedEmailServiceMock.Setup(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()))
            .Callback<QueuedEmail>(e => _queued = e);

        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById(It.IsAny<string>()))
            .ReturnsAsync(new Language { Id = "en", Published = true });
        var emailAccountService = new Mock<IEmailAccountService>();
        emailAccountService.Setup(x => x.GetEmailAccountById(It.IsAny<string>()))
            .ReturnsAsync(new EmailAccount { Id = "ea", Email = "shop@example.com", DisplayName = "Shop" });
        var storeService = new Mock<IStoreService>();

        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetOrderApprovalTokensCommand>(), default))
            .ReturnsAsync(new LiquidOrderApproval { Total = "EUR 120.00" });

        var lookup = new MessageLookupService(_messageTemplateServiceMock.Object, languageService.Object,
            emailAccountService.Object, storeService.Object, new EmailAccountSettings());
        _service = new CustomerMessageProviderService(lookup,
            new MessageProviderService(_queuedEmailServiceMock.Object), _mediatorMock.Object, new CommonSettings());
    }

    private void TemplateExists(string name, string subject, string body)
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(name, "store1"))
            .ReturnsAsync(new MessageTemplate { Name = name, IsActive = true, Subject = subject, Body = body });
    }

    [TestMethod]
    public async Task SendRequested_ToApproverEmail_WithTemplate()
    {
        //Arrange
        TemplateExists(MessageTemplateNames.OrderApprovalRequested, "Approve {{OrderApproval.Total}}",
            "Total {{OrderApproval.Total}}");

        //Act
        var result = await _service.SendOrderApprovalRequestedMessage(_approval, _buyer, _approver, _store, "en");

        //Assert
        Assert.AreEqual(1, result);
        Assert.IsNotNull(_queued);
        Assert.AreEqual("approver@example.com", _queued.To);
        Assert.AreEqual("shop@example.com", _queued.From);
        Assert.AreEqual("Approve EUR 120.00", _queued.Subject);
        Assert.AreEqual("Total EUR 120.00", _queued.Body);
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once);
    }

    [TestMethod]
    public async Task SendRequested_TemplateMissing_ReturnsZero()
    {
        //Act
        var result = await _service.SendOrderApprovalRequestedMessage(_approval, _buyer, _approver, _store, "en");

        //Assert
        Assert.AreEqual(0, result);
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Never);
    }

    [TestMethod]
    public async Task SendRequested_TemplateInactive_ReturnsZero()
    {
        //Arrange
        _messageTemplateServiceMock
            .Setup(x => x.GetMessageTemplateByName(MessageTemplateNames.OrderApprovalRequested, "store1"))
            .ReturnsAsync(new MessageTemplate { IsActive = false });

        //Act
        var result = await _service.SendOrderApprovalRequestedMessage(_approval, _buyer, _approver, _store, "en");

        //Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task SendDecided_ToBuyerEmail()
    {
        //Arrange
        TemplateExists(MessageTemplateNames.OrderApprovalDecided, "Decided", "Body");

        //Act
        var result = await _service.SendOrderApprovalDecidedMessage(_approval, _buyer, _store, "en");

        //Assert
        Assert.AreEqual(1, result);
        Assert.AreEqual("buyer@example.com", _queued.To);
        Assert.AreEqual(Reference.Customer, _queued.Reference);
        Assert.AreEqual("buyer1", _queued.ObjectId);
    }

    [TestMethod]
    public async Task SendDecided_TemplateMissing_ReturnsZero()
    {
        //Act
        var result = await _service.SendOrderApprovalDecidedMessage(_approval, _buyer, _store, "en");

        //Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task SendRequested_LinksUseTheStorePrimaryDomain()
    {
        //Arrange
        TemplateExists(MessageTemplateNames.OrderApprovalRequested, "s", "b");
        var store = new Store {
            Id = "store1", Url = "https://store.example/",
            Domains = {
                new DomainHost { HostName = "primary.example", Url = "https://primary.example/", Primary = true }
            }
        };

        //Act
        await _service.SendOrderApprovalRequestedMessage(_approval, _buyer, _approver, store, "en");

        //Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<GetOrderApprovalTokensCommand>(c => c.Host.Url == "https://primary.example/"), default), Times.Once);
    }

    [TestMethod]
    public async Task SendRequested_NullApproval_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            _service.SendOrderApprovalRequestedMessage(null, _buyer, _approver, _store, "en"));
    }
}
