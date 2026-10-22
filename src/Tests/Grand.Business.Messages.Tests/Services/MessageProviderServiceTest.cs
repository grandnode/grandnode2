using Grand.Business.Core.Events.Messages;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Services;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class MessageProviderServiceTest
{
    private Mock<CommonSettings> _commonSetting;
    private Mock<IEmailAccountService> _emailAccountServiceMock;
    private Mock<EmailAccountSettings> _emailAccountSettingsMock;
    private Mock<IGroupService> _groupService;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMediator> _mediatorMock;
    private MessageProviderService _messageService;
    private CustomerMessageProviderService _customerMessageService;
    private CatalogMessageProviderService _catalogMessageService;
    private VendorMessageProviderService _vendorMessageService;
    private OrderMessageProviderService _orderMessageService;
    private NewsletterMessageProviderService _newsletterMessageService;
    private MerchandiseReturnMessageProviderService _merchandiseReturnMessageService;
    private CmsMessageProviderService _cmsMessageService;
    private Mock<IMessageTemplateService> _messageTemplateServiceMock;
    private Mock<IMessageTokenProvider> _messageTokenProviderMock;
    private Product _product;
    private Mock<IQueuedEmailService> _queuedEmailServiceMock;
    private Mock<IStoreService> _storeServiceMock;

    [TestInitialize]
    public void Init()
    {
        _messageTemplateServiceMock = new Mock<IMessageTemplateService>();
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(new MessageTemplate { IsActive = true }));

        _queuedEmailServiceMock = new Mock<IQueuedEmailService>();
        _groupService = new Mock<IGroupService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(x => x.GetLanguageById(It.IsAny<string>())).Returns(Task.FromResult(new Language()));
        _languageServiceMock.Setup(x => x.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(
            Task.FromResult(
                new List<Language> { new() { Name = "English" }, new() { Name = "Polish" } } as IList<Language>));

        _emailAccountServiceMock = new Mock<IEmailAccountService>();
        _emailAccountServiceMock.Setup(x => x.GetAllEmailAccounts(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<EmailAccount>>(
                new PagedList<EmailAccount>(new List<EmailAccount> { new() { Email = "sdfsdf@mail.com" } }, 0, int.MaxValue)));

        _messageTokenProviderMock = new Mock<IMessageTokenProvider>();

        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(x => x.GetStoreById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Store { Url = "https://localhost:44350/" }));
        _storeServiceMock.Setup(x => x.GetAllStores()).Returns(Task.FromResult(new List<Store>() as IList<Store>));

        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetCustomerByIdQuery>(), default))
            .Returns(Task.FromResult(new Customer { Email = "sdfsdf@mail.com" }));

        _emailAccountSettingsMock = new Mock<EmailAccountSettings>();
        _commonSetting = new Mock<CommonSettings>();

        _messageService = new MessageProviderService(_queuedEmailServiceMock.Object);
        var messageLookupService = new MessageLookupService(
            _messageTemplateServiceMock.Object,
            _languageServiceMock.Object,
            _emailAccountServiceMock.Object,
            _storeServiceMock.Object,
            _emailAccountSettingsMock.Object);
        _customerMessageService = new CustomerMessageProviderService(messageLookupService, _messageService,
            _mediatorMock.Object, _commonSetting.Object);
        _catalogMessageService = new CatalogMessageProviderService(messageLookupService, _messageService,
            _mediatorMock.Object, _storeServiceMock.Object, _commonSetting.Object);
        _vendorMessageService = new VendorMessageProviderService(messageLookupService, _messageService,
            _mediatorMock.Object, _commonSetting.Object);
        _orderMessageService = new OrderMessageProviderService(messageLookupService, _messageService,
            _mediatorMock.Object, _storeServiceMock.Object);
        _newsletterMessageService = new NewsletterMessageProviderService(messageLookupService, _messageService,
            _mediatorMock.Object);
        _merchandiseReturnMessageService = new MerchandiseReturnMessageProviderService(messageLookupService,
            _messageService, _mediatorMock.Object);
        _cmsMessageService = new CmsMessageProviderService(messageLookupService, _messageService,
            _mediatorMock.Object, _groupService.Object);
        _product = new Product();
    }

    [TestMethod]
    public async Task CallInsertQueuedEmailMethod()
    {
        var result = await _catalogMessageService.SendOutBidCustomerMessage(new Product(), "123", new Bid());
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once());
    }

    [TestMethod]
    public async Task SendOutBidCustomerNotificationMethodReturnCorrectResult()
    {
        var result = await _catalogMessageService.SendOutBidCustomerMessage(new Product(), "123", new Bid());
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task SendNewVendorAccountApplyStoreOwnerNotificationRetunsCorrectResult()
    {
        var result = await _vendorMessageService.SendNewVendorAccountApplyStoreOwnerMessage(new Customer(), new Vendor(),
            new Store { Url = "https://localhost:44350/" }, "123");
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task SendNotificationMethodToEmailIsNull()
    {
        var result = await _messageService.SendNotification(new MessageTemplate(),
            new EmailAccount(),
            "123",
            new LiquidObject(),
            toName: "The God",
            toEmailAddress: null);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task SendOrderPaidStoreOwnerMessage_QueuesEmail()
    {
        var result = await _orderMessageService.SendOrderPaidStoreOwnerMessage(new Order(), new Customer(), "123");

        Assert.AreEqual(1, result);
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once());
    }

    [TestMethod]
    public async Task SendNewsLetterSubscriptionActivationMessage_QueuesEmail()
    {
        var result = await _newsletterMessageService.SendNewsLetterSubscriptionActivationMessage(
            new NewsLetterSubscription { Email = "a@b.c" }, "123");

        Assert.AreEqual(1, result);
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once());
    }

    [TestMethod]
    public async Task SendNewMerchandiseReturnStoreOwnerMessage_QueuesEmail()
    {
        var result = await _merchandiseReturnMessageService.SendNewMerchandiseReturnStoreOwnerMessage(
            new MerchandiseReturn(), new Order(), "123");

        Assert.AreEqual(1, result);
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once());
    }

    [TestMethod]
    public async Task SendBlogCommentMessage_QueuesEmail()
    {
        var result = await _cmsMessageService.SendBlogCommentMessage(new BlogPost(), new BlogComment(), "123");

        Assert.AreEqual(1, result);
        _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once());
    }

    [TestMethod]
    public async Task SendCustomerMessage_LinksPointToThePrimaryDomainOfTheStore()
    {
        var store = new Store {
            Url = "https://store.example/",
            Domains = {
                new DomainHost { HostName = "second.example", Url = "https://second.example/" },
                new DomainHost { HostName = "primary.example", Url = "https://primary.example/", Primary = true }
            }
        };

        var url = await PasswordRecoveryUrl(store);

        StringAssert.StartsWith(url, "https://primary.example/");
    }

    [TestMethod]
    public async Task SendCustomerMessage_StoreWithoutDomains_LinksPointToTheStoreUrl()
    {
        var url = await PasswordRecoveryUrl(new Store { Url = "https://store.example/" });

        StringAssert.StartsWith(url, "https://store.example/");
    }

    private async Task<string> PasswordRecoveryUrl(Store store)
    {
        LiquidCustomer liquidCustomer = null;
        _mediatorMock.Setup(x => x.Publish(It.IsAny<EntityTokensAddedEvent<Customer>>(), It.IsAny<CancellationToken>()))
            .Callback<EntityTokensAddedEvent<Customer>, CancellationToken>((e, _) =>
                liquidCustomer = (LiquidCustomer)e.LiquidDrop);

        await _customerMessageService.SendCustomerPasswordRecoveryMessage(new Customer { Email = "a@b.c" }, store, "");

        Assert.IsNotNull(liquidCustomer);
        return liquidCustomer.PasswordRecoveryURL;
    }
}
