using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerSubAccountsTests
{
    private readonly Customer _customer = new() { Id = "owner", Email = "owner@x.com", StoreId = "store-9" };
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private CustomerSettings _customerSettings;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IMediator> _mediatorMock;
    private AccountController _controller;

    [TestInitialize]
    public void Init()
    {
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _customerSettings = new CustomerSettings { HideSubAccountsTab = false };
        _groupServiceMock = new Mock<IGroupService>();
        _groupServiceMock.Setup(g => g.IsOwner(_customer)).ReturnsAsync(true);
        _mediatorMock = new Mock<IMediator>();

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(_customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);

        _controller = new AccountController(
            new Mock<IGrandAuthenticationService>().Object,
            new Mock<ITranslationService>().Object,
            contextAccessor.Object,
            new Mock<ICustomerService>().Object,
            _groupServiceMock.Object,
            _customerManagerServiceMock.Object,
            new Mock<ICountryService>().Object,
            _mediatorMock.Object,
            new Mock<IMessageProviderService>().Object,
            new CaptchaSettings(),
            _customerSettings,
            new CustomerConfig()) {
            ControllerContext = new ControllerContext { HttpContext = HtmlRequest() }
        };
    }

    private static DefaultHttpContext HtmlRequest()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Accept = "text/html";
        return httpContext;
    }

    private void VerifyNoCommand()
    {
        _mediatorMock.Verify(m => m.Send(It.IsAny<SubAccountAddCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<SubAccountEditCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<SubAccountDeleteCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void AssertRedirectsTo(string route, IActionResult result)
    {
        Assert.IsInstanceOfType<RedirectToRouteResult>(result);
        Assert.AreEqual(route, ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task SubAccountActions_TabHidden_RedirectAndChangeNothing()
    {
        _customerSettings.HideSubAccountsTab = true;

        AssertRedirectsTo("CustomerInfo", await _controller.SubAccounts());
        AssertRedirectsTo("CustomerInfo", await _controller.SubAccountAdd());
        AssertRedirectsTo("CustomerInfo", await _controller.SubAccountAdd(new SubAccountCreateModel()));
        AssertRedirectsTo("CustomerInfo", await _controller.SubAccountEdit("c1"));
        AssertRedirectsTo("CustomerInfo", await _controller.SubAccountEdit(new SubAccountEditModel()));
        AssertRedirectsTo("CustomerInfo", await _controller.SubAccountDelete("c1"));
        VerifyNoCommand();
    }

    [TestMethod]
    public async Task SubAccountActions_NotAnOwner_ChallengeAndChangeNothing()
    {
        _groupServiceMock.Setup(g => g.IsOwner(_customer)).ReturnsAsync(false);

        Assert.IsInstanceOfType<ChallengeResult>(await _controller.SubAccountAdd(new SubAccountCreateModel()));
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.SubAccountEdit(new SubAccountEditModel()));
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.SubAccountDelete("c1"));
        VerifyNoCommand();
    }

    [TestMethod]
    public async Task SubAccountEdit_AccountNotAvailable_RedirectsToTheList()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubAccountEditModel)null);

        AssertRedirectsTo("CustomerSubAccounts", await _controller.SubAccountEdit("c1"));
    }

    [TestMethod]
    public async Task ChangePassword_ChangesThePasswordOfTheSignedInAccount()
    {
        await _controller.ChangePassword(new ChangePasswordModel { OldPassword = "old", NewPassword = "new" });

        _customerManagerServiceMock.Verify(m => m.ChangePassword(_customer, It.IsAny<PasswordFormat>(), "new"),
            Times.Once);
        _customerManagerServiceMock.Verify(m => m.ChangePassword(It.IsAny<ChangePasswordRequest>(),
            It.IsAny<string>()), Times.Never);
    }
}
