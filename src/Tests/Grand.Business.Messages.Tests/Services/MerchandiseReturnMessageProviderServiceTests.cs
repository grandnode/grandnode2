using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.Services;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class MerchandiseReturnMessageProviderServiceTests
{
    private readonly MerchandiseReturnNote _note = new() { Id = "n1", Note = "Is it broken?" };
    private readonly Order _order = new() { Id = "o1", StoreId = "store1" };
    private readonly MerchandiseReturn _return = new() { Id = "r1", OrderId = "o1", CustomerId = "c1" };
    private Mock<IMessageTemplateService> _messageTemplateServiceMock;
    private List<QueuedEmail> _queued;
    private MerchandiseReturnMessageProviderService _service;

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
        _service = new MerchandiseReturnMessageProviderService(lookup,
            new MessageProviderService(queuedEmailService.Object), new Mock<IMediator>().Object);
    }

    [TestMethod]
    public async Task SendNoteAddedStoreOwner_QueuesToStoreAccount()
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(
                MessageTemplateNames.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage, "store1"))
            .ReturnsAsync(new MessageTemplate { IsActive = true, Subject = "New message", Body = "Body" });

        var result = await _service.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage(_return, _note, _order, "en");

        Assert.AreEqual(1, result);
        var email = _queued.Single();
        Assert.AreEqual("shop@example.com", email.To);
        Assert.AreEqual("New message", email.Subject);
        Assert.AreEqual(Reference.MerchandiseReturn, email.Reference);
        Assert.AreEqual("r1", email.ObjectId);
    }

    [TestMethod]
    public async Task SendNoteAddedStoreOwner_InactiveTemplate_Zero()
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(
                MessageTemplateNames.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage, "store1"))
            .ReturnsAsync(new MessageTemplate { IsActive = false, Subject = "New message", Body = "Body" });

        var result = await _service.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage(_return, _note, _order, "en");

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, _queued.Count);
    }
}
