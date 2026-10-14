using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Web.Commands.Handler.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Customers;

[TestClass]
public class UpdateCustomerInfoCommandHandlerTests
{
    private Mock<ICustomerService> _customerService;

    private async Task<Customer> Update(OidcLoginConfig config)
    {
        _customerService = new Mock<ICustomerService>();
        var handler = new UpdateCustomerInfoCommandHandler(new Mock<IGrandAuthenticationService>().Object,
            new Mock<IVatService>().Object, new Mock<INewsLetterSubscriptionService>().Object,
            _customerService.Object, new CustomerSettings { AllowUsersToChangeEmail = true }, new TaxSettings(), config);
        var customer = new Customer { Id = "c1", Email = "old@x.com" };
        await handler.Handle(new UpdateCustomerInfoCommand {
            Customer = customer,
            Store = new Domain.Stores.Store { Id = "s1" },
            Model = new CustomerInfoModel { Email = "new@x.com", FirstName = "A", LastName = "B" },
            CustomerAttributes = []
        }, CancellationToken.None);
        return customer;
    }

    [TestMethod]
    public async Task UpdateInfo_ProviderOnly_EmailNotChanged()
    {
        var customer = await Update(new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly });
        Assert.AreEqual("old@x.com", customer.Email);
    }

    [TestMethod]
    public async Task UpdateInfo_Mixed_EmailChanged()
    {
        var customer = await Update(new OidcLoginConfig());
        Assert.AreEqual("new@x.com", customer.Email);
    }
}
