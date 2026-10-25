using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Configuration;
using Grand.Web.Features.Handlers.Customers;
using Grand.Web.Features.Models.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Customers;

[TestClass]
public class GetNavigationHandlerTests
{
    private static Mock<ICustomerService> CustomerServiceWith(int subAccounts)
    {
        var customers = new Mock<ICustomerService>();
        customers.Setup(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(), It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Customer>([], 0, 1, subAccounts));
        return customers;
    }

    private static async Task<bool> HideChangePassword(OidcLoginConfig config)
    {
        var handler = new GetNavigationHandler(new CustomerSettings(), new LoyaltyPointsSettings(),
            new OrderSettings(), new VendorSettings(), config, CustomerServiceWith(0).Object);
        var model = await handler.Handle(new GetNavigation { Customer = new Customer() }, CancellationToken.None);
        return model.HideChangePassword;
    }

    [TestMethod]
    public async Task Navigation_ProviderOnly_HidesChangePassword()
    {
        Assert.IsTrue(await HideChangePassword(new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly }));
    }

    [TestMethod]
    public async Task Navigation_Mixed_ShowsChangePassword()
    {
        Assert.IsFalse(await HideChangePassword(new OidcLoginConfig()));
    }

    private static async Task<bool> HideOrderApprovals(Customer customer, int subAccounts,
        bool featureOff = false)
    {
        var customers = CustomerServiceWith(subAccounts);
        var handler = new GetNavigationHandler(new CustomerSettings { HideSubAccountsTab = featureOff },
            new LoyaltyPointsSettings(), new OrderSettings(), new VendorSettings(), new OidcLoginConfig(),
            customers.Object);
        var model = await handler.Handle(new GetNavigation { Customer = customer }, CancellationToken.None);
        return model.HideOrderApprovals;
    }

    [TestMethod]
    public async Task Navigation_SubAccount_ShowsOrderApprovals()
    {
        Assert.IsFalse(await HideOrderApprovals(new Customer { Id = "sub", OwnerId = "owner" }, 0));
    }

    [TestMethod]
    public async Task Navigation_OwnerWithSubAccounts_ShowsOrderApprovals()
    {
        Assert.IsFalse(await HideOrderApprovals(new Customer { Id = "owner" }, 2));
    }

    [TestMethod]
    public async Task Navigation_OwnerWithoutSubAccounts_HidesOrderApprovals()
    {
        Assert.IsTrue(await HideOrderApprovals(new Customer { Id = "owner" }, 0));
    }

    [TestMethod]
    public async Task Navigation_FeatureOff_HidesOrderApprovals()
    {
        Assert.IsTrue(await HideOrderApprovals(new Customer { Id = "sub", OwnerId = "owner" }, 0, true));
    }
}
