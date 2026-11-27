using Grand.Business.Core.Interfaces.Checkout.Orders;
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

    private static Mock<IRecurringPaymentService> RecurringPaymentsWith(int recurringPayments)
    {
        var recurring = new Mock<IRecurringPaymentService>();
        recurring.Setup(x => x.HasCustomerRecurringPayments("c1", "s1")).ReturnsAsync(recurringPayments > 0);
        return recurring;
    }

    private static async Task<bool> HideChangePassword(OidcLoginConfig config)
    {
        var handler = new GetNavigationHandler(new CustomerSettings(), new LoyaltyPointsSettings(),
            new OrderSettings(), new VendorSettings(), config, CustomerServiceWith(0).Object, RecurringPaymentsWith(0).Object);
        var model = await handler.Handle(new GetNavigation { Language = new Grand.Domain.Localization.Language(), Store = new Grand.Domain.Stores.Store(), Customer = new Customer() }, CancellationToken.None);
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
            customers.Object, RecurringPaymentsWith(0).Object);
        var model = await handler.Handle(new GetNavigation { Language = new Grand.Domain.Localization.Language(), Store = new Grand.Domain.Stores.Store(), Customer = customer }, CancellationToken.None);
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

    private static async Task<bool> HideRecurringPayments(int recurringPayments, bool featureOff = false,
        bool withStore = true)
    {
        var handler = new GetNavigationHandler(new CustomerSettings { HideRecurringPaymentsTab = featureOff },
            new LoyaltyPointsSettings(), new OrderSettings(), new VendorSettings(), new OidcLoginConfig(),
            CustomerServiceWith(0).Object, RecurringPaymentsWith(recurringPayments).Object);
        var model = await handler.Handle(new GetNavigation { Language = new Grand.Domain.Localization.Language(),
            Customer = new Customer { Id = "c1" },
            Store = withStore ? new Grand.Domain.Stores.Store { Id = "s1" } : null
        }, CancellationToken.None);
        return model.HideRecurringPayments;
    }

    [TestMethod]
    public async Task Navigation_CustomerWithRecurringPayment_ShowsRecurringPayments()
    {
        Assert.IsFalse(await HideRecurringPayments(1));
    }

    [TestMethod]
    public async Task Navigation_CustomerWithoutRecurringPayments_HidesRecurringPayments()
    {
        Assert.IsTrue(await HideRecurringPayments(0));
    }

    [TestMethod]
    public async Task Navigation_RecurringPaymentsTabOff_HidesRecurringPayments()
    {
        Assert.IsTrue(await HideRecurringPayments(1, featureOff: true));
    }

    [TestMethod]
    public async Task Navigation_NoStore_HidesRecurringPayments()
    {
        Assert.IsTrue(await HideRecurringPayments(1, withStore: false));
    }
}
