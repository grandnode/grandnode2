using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Marketing.Services.Campaigns;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Campaigns;

[TestClass]
public class CampaignServiceTests
{
    private IRepository<CampaignHistory> _campaignHistoryRepository;
    private IRepository<Campaign> _campaignRepository;
    private CampaignService _campaignService;
    private IRepository<Customer> _customerRepository;
    private Mock<IEmailSender> _emailSenderMock;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMediator> _mediatorMock;
    private IRepository<NewsLetterSubscription> _newsLetterSubscriptionRepository;
    private Mock<IQueuedEmailService> _queuedEmailServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _campaignRepository = new MongoDBRepositoryTest<Campaign>();
        _campaignHistoryRepository = new MongoDBRepositoryTest<CampaignHistory>();
        _newsLetterSubscriptionRepository = new MongoDBRepositoryTest<NewsLetterSubscription>();
        _customerRepository = new MongoDBRepositoryTest<Customer>();
        _mediatorMock = new Mock<IMediator>();
        _emailSenderMock = new Mock<IEmailSender>();
        _queuedEmailServiceMock = new Mock<IQueuedEmailService>();
        _storeServiceMock = new Mock<IStoreService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _workContextMock = new Mock<IWorkContext>();
        _campaignService = new CampaignService(_campaignRepository, _campaignHistoryRepository,
            _newsLetterSubscriptionRepository, _customerRepository,
            _emailSenderMock.Object, _queuedEmailServiceMock.Object, _storeServiceMock.Object, _mediatorMock.Object,
            _languageServiceMock.Object, _workContextMock.Object);
    }

    [TestMethod]
    public async Task InsertCampaignTest()
    {
        //Act
        await _campaignService.InsertCampaign(new Campaign());
        //Assert
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Campaign>>(), default), Times.Once);
        Assert.IsTrue(_campaignRepository.Table.Any());
    }

    [TestMethod]
    public async Task InsertCampaignHistoryTest()
    {
        //Act
        await _campaignService.InsertCampaignHistory(new CampaignHistory());
        //Assert
        Assert.IsTrue(_campaignHistoryRepository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCampaignTest()
    {
        //Arrange
        var campaign = new Campaign();
        await _campaignService.InsertCampaign(campaign);

        //Act
        campaign.Subject = "test";
        await _campaignService.UpdateCampaign(campaign);
        //Assert
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Campaign>>(), default), Times.Once);
        Assert.IsTrue(_campaignRepository.Table.FirstOrDefault(x => x.Id == campaign.Id).Subject == "test");
    }

    [TestMethod]
    public async Task DeleteCampaignTest()
    {
        //Arrange
        var campaign = new Campaign();
        await _campaignService.InsertCampaign(campaign);

        //Act
        await _campaignService.DeleteCampaign(campaign);
        //Assert
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Campaign>>(), default), Times.Once);
        Assert.IsFalse(_campaignRepository.Table.Any());
    }

    [TestMethod]
    public async Task GetCampaignByIdTest()
    {
        //Arrange
        var campaign = new Campaign {
            Name = "test"
        };
        await _campaignRepository.InsertAsync(campaign);

        //Act
        var result = await _campaignService.GetCampaignById(campaign.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task GetAllCampaignsTest()
    {
        //Arrange
        await _campaignRepository.InsertAsync(new Campaign());
        await _campaignRepository.InsertAsync(new Campaign());
        await _campaignRepository.InsertAsync(new Campaign());

        //Act
        var result = await _campaignService.GetAllCampaigns();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetCampaignHistoryTest()
    {
        //Arrange
        var campaign = new Campaign {
            Name = "test"
        };
        await _campaignRepository.InsertAsync(campaign);
        await _campaignHistoryRepository.InsertAsync(new CampaignHistory { CampaignId = campaign.Id });
        await _campaignHistoryRepository.InsertAsync(new CampaignHistory { CampaignId = campaign.Id });

        //Act
        var result = await _campaignService.GetCampaignHistory(campaign);

        //Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task CustomerSubscriptionsTest()
    {
        //Arrange
        var campaign = new Campaign {
            Name = "test",
            StoreId = "1"
        };
        await _campaignRepository.InsertAsync(campaign);
        await _newsLetterSubscriptionRepository.InsertAsync(new NewsLetterSubscription
            { StoreId = "1", Active = true });
        //Act
        var result = await _campaignService.CustomerSubscriptions(campaign);

        //Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task SendCampaignTest()
    {
        //Arrange
        var campaign = new Campaign {
            Name = "test",
            StoreId = "1"
        };
        await _campaignRepository.InsertAsync(campaign);
        await _newsLetterSubscriptionRepository.InsertAsync(new NewsLetterSubscription
            { StoreId = "1", Active = true, Email = "test@test.com" });

        _languageServiceMock.Setup(x => x.GetLanguageById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Language()));
        _storeServiceMock.Setup(x => x.GetStoreById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Store()));

        //Act
        await _campaignService.SendCampaign(campaign, new EmailAccount { Host = "host.com" }, "test@test.com");

        //Assert
        _emailSenderMock.Verify(
            c => c.SendEmail(It.IsAny<EmailAccount>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, null, null, null, null),
            Times.Once);
    }
}