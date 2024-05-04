using Grand.Business.Core.Events.Marketing;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Marketing.Services.Newsletters;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Infrastructure.Events;
using Grand.SharedKernel;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Newsletters;

[TestClass]
public class NewsLetterSubscriptionServiceTests
{
    private Mock<IHistoryService> _historyServiceMock;
    private Mock<IMediator> _mediatorMock;
    private INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private Mock<IRepository<NewsLetterSubscription>> _subscriptionRepository;

    [TestInitialize]
    public void TestInitialize()
    {
        _mediatorMock = new Mock<IMediator>();
        _subscriptionRepository = new Mock<IRepository<NewsLetterSubscription>>();
        _historyServiceMock = new Mock<IHistoryService>();
        _newsLetterSubscriptionService = new NewsLetterSubscriptionService(_subscriptionRepository.Object,
            _mediatorMock.Object, _historyServiceMock.Object);
    }


    [TestMethod]
    public void InsertNewsLetterSubscription_InvalidEmail_ThrowException()
    {
        var email = "NotValidEmail";
        var newsLetterSubscription = new NewsLetterSubscription {
            Email = email
        };
        Assert.ThrowsExceptionAsync<GrandException>(async () =>
            await _newsLetterSubscriptionService.InsertNewsLetterSubscription(newsLetterSubscription));
    }

    [TestMethod]
    public async Task InsertNewsLetterSubscription_ActiveSubcription_InvokeRepositoryAndPublishSubscriptionEvent()
    {
        var email = "johny@gmail.com";
        var newsLetterSubscription = new NewsLetterSubscription { Email = email, Active = true };
        await _newsLetterSubscriptionService.InsertNewsLetterSubscription(newsLetterSubscription);
        _subscriptionRepository.Verify(r => r.InsertAsync(newsLetterSubscription), Times.Once);
        _historyServiceMock.Verify(h => h.SaveObject(It.IsAny<BaseEntity>()), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<EmailSubscribedEvent>(), default), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<NewsLetterSubscription>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task InsertNewsLetterSubscription_InactiveSubcription_InvokeRepository()
    {
        var email = "johny@gmail.com";
        var newsLetterSubscription = new NewsLetterSubscription { Email = email, Active = false };
        await _newsLetterSubscriptionService.InsertNewsLetterSubscription(newsLetterSubscription);
        _subscriptionRepository.Verify(r => r.InsertAsync(newsLetterSubscription), Times.Once);
        _historyServiceMock.Verify(h => h.SaveObject(It.IsAny<BaseEntity>()), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<EmailSubscribedEvent>(), default), Times.Never);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<NewsLetterSubscription>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateNewsLetterSubscription_InvokeRepository()
    {
        var email = "johny@gmail.com";
        var newsLetterSubscription = new NewsLetterSubscription { Email = email, Active = false };
        await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(newsLetterSubscription);
        _subscriptionRepository.Verify(r => r.UpdateAsync(newsLetterSubscription), Times.Once);
        _historyServiceMock.Verify(h => h.SaveObject(It.IsAny<BaseEntity>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<NewsLetterSubscription>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteNewsLetterSubscription_InvokeRepositoryAndEmailUnsubscribedEvent()
    {
        var email = "johny@gmail.com";
        var newsLetterSubscription = new NewsLetterSubscription { Email = email, Active = false };
        await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsLetterSubscription);
        _subscriptionRepository.Verify(r => r.DeleteAsync(newsLetterSubscription), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<EmailUnsubscribedEvent>(), default), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<NewsLetterSubscription>>(), default), Times.Once);
    }
}