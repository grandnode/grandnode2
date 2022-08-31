using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.Services;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services
{
    [TestClass()]
    public class MessageProviderServiceTest
    {
        private Mock<IMessageTemplateService> _messageTemplateServiceMock;
        private Mock<IQueuedEmailService> _queuedEmailServiceMock;
        private Mock<IGroupService> _groupService;
        private Mock<ILanguageService> _languageServiceMock;
        private Mock<IEmailAccountService> _emailAccountServiceMock;
        private Mock<IMessageTokenProvider> _messageTokenProviderMock;
        private Mock<IStoreService> _storeServiceMock;
        private Mock<IStoreHelper> _storeHelperServiceMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<EmailAccountSettings> _emailAccountSettingsMock;
        private Mock<CommonSettings> _commonSetting;
        private MessageProviderService _messageService;
        private Product _product;

        [TestInitialize()]
        public void Init()
        {
            _messageTemplateServiceMock = new Mock<IMessageTemplateService>();
            _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new MessageTemplate { IsActive = true }));

            _queuedEmailServiceMock = new Mock<IQueuedEmailService>();
            _groupService = new Mock<IGroupService>();
            _languageServiceMock = new Mock<ILanguageService>();
            _languageServiceMock.Setup(x => x.GetLanguageById(It.IsAny<string>())).Returns(Task.FromResult(new Language()));
            _languageServiceMock.Setup(x => x.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.FromResult(
                new List<Language> { new Language { Name = "English" }, new Language { Name = "Polish" } } as IList<Language>));

            _emailAccountServiceMock = new Mock<IEmailAccountService>();
            _emailAccountServiceMock.Setup(x => x.GetAllEmailAccounts())
                .Returns(Task.FromResult(new List<EmailAccount> { new EmailAccount { Email = "sdfsdf@mail.com" } } as IList<EmailAccount>));

            _messageTokenProviderMock = new Mock<IMessageTokenProvider>();

            _storeServiceMock = new Mock<IStoreService>();
            _storeServiceMock.Setup(x => x.GetStoreById(It.IsAny<string>())).Returns(Task.FromResult(new Store() { Url = "https://localhost:44350/" }));
            _storeServiceMock.Setup(x => x.GetAllStores()).Returns(Task.FromResult(new List<Store>() as IList<Store>));

            _storeHelperServiceMock = new Mock<IStoreHelper>();

            _mediatorMock = new Mock<IMediator>();
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetCustomerByIdQuery>(), default))
                .Returns(Task.FromResult(new Customer { Email = "sdfsdf@mail.com" }));

            _emailAccountSettingsMock = new Mock<EmailAccountSettings>();
            _commonSetting = new Mock<CommonSettings>();

            _messageService = new MessageProviderService(
                _messageTemplateServiceMock.Object,
                _queuedEmailServiceMock.Object,
                _languageServiceMock.Object,
                _emailAccountServiceMock.Object,
                _storeServiceMock.Object,
                _storeHelperServiceMock.Object,
                _groupService.Object,
                _mediatorMock.Object,
                _emailAccountSettingsMock.Object,
                _commonSetting.Object);
            _product = new Product();

        }

        [TestMethod()]
        public async Task CallInsertQueuedEmailMethod()
        {
            var result = await _messageService.SendOutBidCustomerMessage(new Product(), "123", new Bid());
            _queuedEmailServiceMock.Verify(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>()), Times.Once());
        }

        [TestMethod()]
        public async Task SendOutBidCustomerNotificationMethodReturnCorrectResult()
        {
            var result = await _messageService.SendOutBidCustomerMessage(new Product(), "123", new Bid());
            Assert.AreEqual(result, 1);
        }

        [TestMethod()]
        public async Task SendNewVendorAccountApplyStoreOwnerNotificationRetunsCorrectResult()
        {
            var result = await _messageService.SendNewVendorAccountApplyStoreOwnerMessage(new Customer(), new Vendor(), new Store() { Url = "https://localhost:44350/" }, "123");
            Assert.AreEqual(result, 1);
        }

        [TestMethod()]
        public async Task SendNotificationMethodToEmailIsNull()
        {
            var result = await _messageService.SendNotification(messageTemplate: new MessageTemplate(),
                emailAccount: new EmailAccount(),
                languageId: "123",
                liquidObject: new LiquidObject(),
                toName: "The God",
                toEmailAddress: null);

            Assert.AreEqual(result, 0);
        }
    }
}
