using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Messages.Services;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

/// <summary>
///     The e-mail with the link that opens an order placed without an account
/// </summary>
[TestClass]
public class OrderMessageProviderServiceGuestLinkTests
{
    private Mock<IMessageTemplateService> _messageTemplateServiceMock;
    private List<QueuedEmail> _queued;
    private OrderMessageProviderService _service;

    [TestInitialize]
    public void Init()
    {
        _queued = [];
        _messageTemplateServiceMock = new Mock<IMessageTemplateService>();
        var queuedEmailService = new Mock<IQueuedEmailService>();
        queuedEmailService.Setup(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>())).Callback<QueuedEmail>(_queued.Add);

        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById(It.IsAny<string>()))
            .ReturnsAsync(new Language { Id = "en", Published = true });
        var emailAccountService = new Mock<IEmailAccountService>();
        emailAccountService.Setup(x => x.GetEmailAccountById(It.IsAny<string>()))
            .ReturnsAsync(new EmailAccount { Id = "ea", Email = "shop@example.com", DisplayName = "Shop" });
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("store1"))
            .ReturnsAsync(new Store { Id = "store1", Url = "https://store.example/" });

        var lookup = new MessageLookupService(_messageTemplateServiceMock.Object, languageService.Object,
            emailAccountService.Object, storeService.Object, new EmailAccountSettings());
        _service = new OrderMessageProviderService(lookup, new MessageProviderService(queuedEmailService.Object),
            new Mock<IDownloadService>().Object, new Mock<IMediator>().Object,
            NullLogger<OrderMessageProviderService>.Instance);
    }

    [TestMethod]
    public async Task SendGuestLink_QueuesToOrderEmailWithLink()
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(
                MessageTemplateNames.SendGuestOrderLinkCustomerMessage, "store1"))
            .ReturnsAsync(new MessageTemplate
                { IsActive = true, Subject = "Return items", Body = "{{GuestOrderLink.Url}}" });
        var order = new Order {
            Id = "o1", StoreId = "store1", OrderNumber = 7, CustomerEmail = "guest@example.com",
            CustomerLanguageId = "en"
        };

        var result = await _service.SendGuestOrderLinkCustomerMessage(order, "a+b");

        Assert.AreEqual(1, result);
        var email = _queued.Single();
        Assert.AreEqual("guest@example.com", email.To);
        StringAssert.Contains(email.Body, "https://store.example/guestorder/access?token=a%2Bb");
        Assert.AreEqual(Reference.Order, email.Reference);
        Assert.AreEqual("o1", email.ObjectId);
    }

    [TestMethod]
    public async Task SendGuestLink_NoCustomerEmail_ToBillingEmail()
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(
                MessageTemplateNames.SendGuestOrderLinkCustomerMessage, "store1"))
            .ReturnsAsync(new MessageTemplate { IsActive = true, Subject = "Return items", Body = "Body" });
        var order = new Order {
            Id = "o1", StoreId = "store1",
            BillingAddress = new Address { Email = "guest@example.com", FirstName = "Gina" }
        };

        await _service.SendGuestOrderLinkCustomerMessage(order, "token");

        Assert.AreEqual("guest@example.com", _queued.Single().To);
    }

    [TestMethod]
    public async Task SendGuestLink_InactiveTemplate_Zero()
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(
                MessageTemplateNames.SendGuestOrderLinkCustomerMessage, "store1"))
            .ReturnsAsync(new MessageTemplate { IsActive = false, Subject = "Return items", Body = "Body" });

        var result = await _service.SendGuestOrderLinkCustomerMessage(
            new Order { Id = "o1", StoreId = "store1", CustomerEmail = "guest@example.com" }, "token");

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, _queued.Count);
    }
}
