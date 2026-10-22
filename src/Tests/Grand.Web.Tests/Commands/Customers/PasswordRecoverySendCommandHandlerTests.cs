using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Interfaces.Authentication;
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
    [TestMethod]
    public async Task ExternalSignInRequired_NoMessage_NoToken()
    {
        var customerService = new Mock<ICustomerService>();
        var messages = new Mock<ICustomerMessageProviderService>();
        var policy = new Mock<IExternalSignInPolicy>();
        var customer = new Customer { Id = "c1" };
        policy.Setup(x => x.PasswordSignIn(customer, It.IsAny<SignInSurface>())).ReturnsAsync(new PasswordSignInDecision(false, new Mock<IExternalAuthenticationProvider>().Object));
        var handler = new PasswordRecoverySendCommandHandler(customerService.Object, messages.Object, policy.Object);

        Assert.IsTrue(await handler.Handle(new PasswordRecoverySendCommand {
            Customer = customer, Store = new Grand.Domain.Stores.Store(), Language = new Language { Id = "l1" }
        }, CancellationToken.None));

        messages.Verify(x => x.SendCustomerPasswordRecoveryMessage(It.IsAny<Customer>(), It.IsAny<Grand.Domain.Stores.Store>(), It.IsAny<string>()), Times.Never);
        customerService.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task NullCustomer_NoMessage()
    {
        var messages = new Mock<ICustomerMessageProviderService>();
        var handler = new PasswordRecoverySendCommandHandler(new Mock<ICustomerService>().Object, messages.Object,
            new Mock<IExternalSignInPolicy>().Object);

        Assert.IsTrue(await handler.Handle(new PasswordRecoverySendCommand {
            Store = new Grand.Domain.Stores.Store(), Language = new Language { Id = "l1" }
        }, CancellationToken.None));

        messages.Verify(x => x.SendCustomerPasswordRecoveryMessage(It.IsAny<Customer>(), It.IsAny<Grand.Domain.Stores.Store>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PasswordAllowed_MessageSent()
    {
        var messages = new Mock<ICustomerMessageProviderService>();
        var policy = new Mock<IExternalSignInPolicy>();
        policy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.Storefront))
            .ReturnsAsync(PasswordSignInDecision.Password);
        var handler = new PasswordRecoverySendCommandHandler(new Mock<ICustomerService>().Object, messages.Object,
            policy.Object);
        await handler.Handle(new PasswordRecoverySendCommand {
            Customer = new Customer { Id = "c1" }, Store = new Grand.Domain.Stores.Store(), Language = new Language { Id = "l1" }
        }, CancellationToken.None);
        messages.Verify(x => x.SendCustomerPasswordRecoveryMessage(It.IsAny<Customer>(), It.IsAny<Grand.Domain.Stores.Store>(), "l1"), Times.Once);
    }
}
