using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerSubAccountsTests
{
    private Customer _currentCustomer;
    private CustomerSettings _customerSettings;
    private Mock<IMediator> _mediator;
    private AccountController _controller;

    [TestInitialize]
    public void Init()
    {
        _currentCustomer = new Customer { Id = "owner" };
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(() => _currentCustomer);
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        //the real rule: an account without an owner is an owner
        var groups = new Mock<IGroupService>();
        groups.Setup(g => g.IsOwner(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => string.IsNullOrEmpty(c.OwnerId));

        _mediator = new Mock<IMediator>();
        _customerSettings = new CustomerSettings();
        _controller = new AccountController(new Mock<IGrandAuthenticationService>().Object,
            new Mock<IRequestTranslationService>().Object, context.Object, new Mock<ICustomerService>().Object,
            groups.Object, new Mock<ICustomerManagerService>().Object, new Mock<ICountryService>().Object,
            _mediator.Object, new Mock<ICustomerMessageProviderService>().Object, new CaptchaSettings(),
            _customerSettings, new CustomerConfig());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, new Mock<ITempDataProvider>().Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("/subaccounts");
        _controller.Url = url.Object;
    }

    private static void AssertRedirectTo(IActionResult result, string routeName)
    {
        Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));
        Assert.AreEqual(routeName, ((RedirectToRouteResult)result).RouteName);
    }

    private static object JsonValue(IActionResult result, string property)
    {
        var value = ((JsonResult)result).Value!;
        return value.GetType().GetProperty(property)!.GetValue(value);
    }

    //hiding the tab switches the feature off - the actions behind it used to stay reachable by URL

    [TestMethod]
    public async Task SubAccountAddPost_TabHidden_CreatesNothing()
    {
        _customerSettings.HideSubAccountsTab = true;

        var result = await _controller.SubAccountAdd(new SubAccountCreateModel { Email = "a@b.com" });

        AssertRedirectTo(result, "CustomerInfo");
        _mediator.Verify(m => m.Send(It.IsAny<SubAccountAddCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SubAccountEditPost_TabHidden_ChangesNothing()
    {
        _customerSettings.HideSubAccountsTab = true;

        var result = await _controller.SubAccountEdit(new SubAccountEditModel { Id = "sub" });

        AssertRedirectTo(result, "CustomerInfo");
        _mediator.Verify(m => m.Send(It.IsAny<SubAccountEditCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SubAccountDelete_TabHidden_DeletesNothing()
    {
        _customerSettings.HideSubAccountsTab = true;

        var result = await _controller.SubAccountDelete("sub");

        AssertRedirectTo(result, "CustomerInfo");
        _mediator.Verify(m => m.Send(It.IsAny<SubAccountDeleteCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SubAccountGetActions_TabHidden_RedirectToCustomerInfo()
    {
        _customerSettings.HideSubAccountsTab = true;

        AssertRedirectTo(await _controller.SubAccounts(), "CustomerInfo");
        AssertRedirectTo(await _controller.SubAccountAdd(), "CustomerInfo");
        AssertRedirectTo(await _controller.SubAccountEdit("sub"), "CustomerInfo");
    }

    [TestMethod]
    public async Task SubAccountAddPost_CalledBySubAccount_IsChallenged()
    {
        _currentCustomer.OwnerId = "someone";

        var result = await _controller.SubAccountAdd(new SubAccountCreateModel { Email = "a@b.com" });

        Assert.IsInstanceOfType(result, typeof(ChallengeResult));
        _mediator.Verify(m => m.Send(It.IsAny<SubAccountAddCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SubAccountDelete_CalledBySubAccount_IsChallenged()
    {
        _currentCustomer.OwnerId = "someone";

        var result = await _controller.SubAccountDelete("sub");

        Assert.IsInstanceOfType(result, typeof(ChallengeResult));
    }

    [TestMethod]
    public async Task SubAccountEditGet_SomeoneElsesAccount_RedirectsToList()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetSubAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubAccountEditModel)null);

        var result = await _controller.SubAccountEdit("foreign");

        AssertRedirectTo(result, "CustomerSubAccounts");
    }

    [TestMethod]
    public async Task SubAccountEditGet_OwnAccount_ShowsIt()
    {
        var model = new SubAccountEditModel { Id = "sub" };
        _mediator.Setup(m => m.Send(It.IsAny<GetSubAccount>(), It.IsAny<CancellationToken>())).ReturnsAsync(model);

        var result = await _controller.SubAccountEdit("sub");

        Assert.AreSame(model, ((ViewResult)result).Model);
    }

    [TestMethod]
    public async Task SubAccountDelete_Refused_ReturnsTheHandlersError()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SubAccountDeleteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "not yours"));

        var result = await _controller.SubAccountDelete("foreign");

        Assert.AreEqual(false, JsonValue(result, "success"));
        Assert.AreEqual("not yours", JsonValue(result, "error"));
    }

    [TestMethod]
    public async Task SubAccountDelete_Done_ReportsSuccess()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SubAccountDeleteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));

        var result = await _controller.SubAccountDelete("sub");

        Assert.AreEqual(true, JsonValue(result, "success"));
        Assert.AreEqual("/subaccounts", JsonValue(result, "redirect"));
    }
}
