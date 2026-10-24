using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Web.Commands.Handler.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Tests.Commands.Customers;

[TestClass]
public class UpdateCustomerInfoCommandHandlerTests
{
    private Mock<ICustomerService> _customerService;

    private async Task<Customer> Update(OidcLoginConfig config, CustomerSettings settings = null,
        Customer customer = null, string email = "new@x.com", string username = null)
    {
        _customerService = new Mock<ICustomerService>();
        var handler = new UpdateCustomerInfoCommandHandler(new Mock<IGrandAuthenticationService>().Object,
            new Mock<IVatService>().Object, new Mock<INewsLetterSubscriptionService>().Object,
            _customerService.Object, settings ?? new CustomerSettings { AllowUsersToChangeEmail = true },
            new TaxSettings(), config);
        customer ??= new Customer { Id = "c1", Email = "old@x.com", Username = "old@x.com" };
        await handler.Handle(new UpdateCustomerInfoCommand {
            Customer = customer,
            Store = new Domain.Stores.Store { Id = "s1" },
            Model = new CustomerInfoModel { Email = email, Username = username, FirstName = "A", LastName = "B" },
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

    [TestMethod]
    public async Task UpdateInfo_MixedCaseEmail_IsStoredLowercased()
    {
        //stored as typed, the lowercased sign-in lookup missed it and the customer could no longer sign in
        var customer = await Update(new OidcLoginConfig(), email: "  New.Me@X.COM ");

        Assert.AreEqual("new.me@x.com", customer.Email);
        _customerService.Verify(c => c.UpdateCustomerField(customer,
            It.IsAny<Expression<Func<Customer, string>>>(), "new.me@x.com"), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task UpdateInfo_NewEmail_UsernamesDisabled_UsernameFollows()
    {
        var customer = await Update(new OidcLoginConfig());

        Assert.AreEqual("new@x.com", customer.Username);
    }

    [TestMethod]
    public async Task UpdateInfo_SameEmailInOtherCase_ChangesNothing()
    {
        var customer = await Update(new OidcLoginConfig(), email: "OLD@X.com");

        Assert.AreEqual("old@x.com", customer.Email);
        _customerService.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow(false, OidcSignInMode.Mixed)]
    [DataRow(true, OidcSignInMode.ProviderOnly)]
    public async Task UpdateInfo_EmailStoredInMixedCaseByOlderEdits_IsRepaired(bool allowChange, OidcSignInMode mode)
    {
        //the same address, so repaired even where changing the e-mail is not allowed
        var customer = await Update(new OidcLoginConfig { SignInMode = mode },
            new CustomerSettings { AllowUsersToChangeEmail = allowChange },
            new Customer { Id = "c1", Email = "Old@X.com", Username = "Old@X.com" }, email: "Old@X.com");

        Assert.AreEqual("old@x.com", customer.Email);
    }

    [TestMethod]
    public async Task UpdateInfo_EmailChangeNotAllowed_OtherAddressIsNotTaken()
    {
        var customer = await Update(new OidcLoginConfig(), new CustomerSettings { AllowUsersToChangeEmail = false });

        Assert.AreEqual("old@x.com", customer.Email);
    }

    [TestMethod]
    public async Task UpdateInfo_MixedCaseUsername_IsStoredLowercased()
    {
        var settings = new CustomerSettings { UsernamesEnabled = true, AllowUsersToChangeUsernames = true };
        var customer = await Update(new OidcLoginConfig(), settings,
            new Customer { Id = "c1", Email = "old@x.com", Username = "old" }, email: "old@x.com", username: " John ");

        Assert.AreEqual("john", customer.Username);
        //with usernames on, the e-mail does not drive the username
        Assert.AreEqual("old@x.com", customer.Email);
    }

    [TestMethod]
    public async Task UpdateInfo_SameUsernameInOtherCase_ChangesNothing()
    {
        var settings = new CustomerSettings { UsernamesEnabled = true, AllowUsersToChangeUsernames = true };
        await Update(new OidcLoginConfig(), settings,
            new Customer { Id = "c1", Email = "old@x.com", Username = "john" }, email: "old@x.com", username: "John");

        _customerService.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()), Times.Never);
    }
}
