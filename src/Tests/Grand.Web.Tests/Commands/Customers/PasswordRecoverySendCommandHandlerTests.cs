using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Commands.Handler.Customers;
using Grand.Web.Commands.Models.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Customers;

[TestClass]
public class PasswordRecoverySendCommandHandlerTests
{
    private readonly Customer _customer = new() { Id = "c1", Active = true };
    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groupService;
    private PasswordRecoverySendCommandHandler _handler;
    private Mock<ICustomerMessageProviderService> _messages;
    private Mock<IExternalSignInPolicy> _policy;

    public PasswordRecoverySendCommandHandlerTests()
    {
        _customerService = new Mock<ICustomerService>();
        _messages = new Mock<ICustomerMessageProviderService>();
        _policy = new Mock<IExternalSignInPolicy>();
        _policy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.Storefront))
            .ReturnsAsync(PasswordSignInDecision.Password);
        _groupService = new Mock<IGroupService>();
        _groupService.Setup(x => x.IsRegistered(_customer)).ReturnsAsync(true);
        _handler = new PasswordRecoverySendCommandHandler(_customerService.Object, _messages.Object, _policy.Object,
            _groupService.Object);
    }

    private Task<bool> Send(Customer? customer) =>
        _handler.Handle(new PasswordRecoverySendCommand { Host = new Grand.Domain.Stores.DomainHost(), Model = new Grand.Web.Models.Customer.PasswordRecoveryModel(),
            Customer = customer, Store = new Grand.Domain.Stores.Store(), Language = new Language { Id = "l1" }
        }, CancellationToken.None);

    private void VerifyNoMessage()
    {
        _messages.Verify(x => x.SendCustomerPasswordRecoveryMessage(It.IsAny<Customer>(),
            It.IsAny<Grand.Domain.Stores.Store>(), It.IsAny<string>()), Times.Never);
        _customerService.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ExternalSignInRequired_NoMessage_NoToken()
    {
        _policy.Setup(x => x.PasswordSignIn(_customer, It.IsAny<SignInSurface>()))
            .ReturnsAsync(new PasswordSignInDecision(false, new Mock<IExternalAuthenticationProvider>().Object));

        Assert.IsTrue(await Send(_customer));

        VerifyNoMessage();
    }

    [TestMethod]
    public async Task NullCustomer_NoMessage()
    {
        Assert.IsTrue(await Send(null));

        VerifyNoMessage();
    }

    [TestMethod]
    public async Task InactiveCustomer_NoMessage()
    {
        _customer.Active = false;

        Assert.IsTrue(await Send(_customer));

        VerifyNoMessage();
    }

    [TestMethod]
    public async Task DeletedCustomer_NoMessage()
    {
        _customer.Deleted = true;

        Assert.IsTrue(await Send(_customer));

        VerifyNoMessage();
    }

    [TestMethod]
    public async Task LockedOutCustomer_NoMessage()
    {
        _customer.CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(10);

        Assert.IsTrue(await Send(_customer));

        VerifyNoMessage();
    }

    [TestMethod]
    public async Task NotRegistered_NoMessage()
    {
        _groupService.Setup(x => x.IsRegistered(_customer)).ReturnsAsync(false);

        Assert.IsTrue(await Send(_customer));

        VerifyNoMessage();
    }

    [TestMethod]
    public async Task PasswordAllowed_MessageSent()
    {
        await Send(_customer);

        _messages.Verify(x => x.SendCustomerPasswordRecoveryMessage(_customer, It.IsAny<Grand.Domain.Stores.Store>(),
            "l1", It.IsAny<Grand.Domain.Stores.DomainHost>()), Times.Once);
    }
}