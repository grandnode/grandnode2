using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Configuration;
using Grand.Web.Features.Handlers.Customers;
using Grand.Web.Features.Models.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Tests.Features.Handlers.Customers;

[TestClass]
public class GetNavigationHandlerTests
{
    private static async Task<bool> HideChangePassword(OidcLoginConfig config)
    {
        var handler = new GetNavigationHandler(new CustomerSettings(), new LoyaltyPointsSettings(),
            new OrderSettings(), new VendorSettings(), config);
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
}
