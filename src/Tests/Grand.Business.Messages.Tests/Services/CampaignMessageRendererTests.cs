using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.Services;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class CampaignMessageRendererTests
{
    private readonly DomainHost _host = new() { Url = "https://localhost/" };
    private readonly Store _store = new() { Url = "https://localhost/" };
    private Mock<IMediator> _mediatorMock;
    private CampaignMessageRenderer _renderer;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _renderer = new CampaignMessageRenderer(_mediatorMock.Object);
    }

    [TestMethod]
    public async Task Render_WithSubscriptionAndQueuedEmail_RendersTheirTokens()
    {
        var campaign = new Campaign {
            Subject = "Hi {{NewsLetterSubscription.Email}}",
            Body = "{{Customer.Email}} {{Email.Id}}"
        };
        var subscription = new NewsLetterSubscription { Email = "subscriber@test.com" };
        var customer = new Customer { Email = "customer@test.com" };

        var result = await _renderer.Render(campaign, _store, new Language(), new EmailAccount(), _host, customer,
            new Currency(), TaxDisplayType.IncludingTax, subscription, "queued-email-id");

        Assert.AreEqual("Hi subscriber@test.com", result.Subject);
        Assert.AreEqual("customer@test.com queued-email-id", result.Body);
        _mediatorMock.Verify(c => c.Send(It.IsAny<GetShoppingCartTokensCommand>(), default), Times.Once);
    }

    [TestMethod]
    public async Task Render_WithoutCustomerSubscriptionAndQueuedEmail_SkipsTheirTokens()
    {
        var campaign = new Campaign {
            Subject = "Subject",
            Body = "[{{NewsLetterSubscription.Email}}][{{Customer.Email}}][{{Email.Id}}]"
        };

        var result = await _renderer.Render(campaign, _store, new Language(), new EmailAccount(), _host, null,
            new Currency(), TaxDisplayType.IncludingTax);

        Assert.AreEqual("Subject", result.Subject);
        Assert.AreEqual("[][][]", result.Body);
        _mediatorMock.Verify(c => c.Send(It.IsAny<GetStoreTokensCommand>(), default), Times.Once);
        _mediatorMock.Verify(c => c.Send(It.IsAny<GetShoppingCartTokensCommand>(), default), Times.Never);
    }
}
