using Grand.Business.Authentication.Services;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class TwoFactorSignInServiceTests
{
    private const TwoFactorAuthenticationType Type = TwoFactorAuthenticationType.AppVerification;

    private Customer _stored;
    private Mock<ICustomerService> _customerService;
    private Mock<ITwoFactorAuthenticationService> _twoFactor;
    private Mock<IResourceLock> _resourceLock;
    private Mock<IMediator> _mediator;
    private CustomerSettings _settings;
    private TwoFactorSignInService _service;

    [TestInitialize]
    public void Init()
    {
        _stored = new Customer { Id = "c1" };
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(() => _stored);
        //stores the field on the customer, as the real service does
        _customerService.Setup(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>()))
            .Callback((Customer c, string key, int value, string _) =>
            {
                foreach (var field in c.UserFields.Where(f => f.Key == key).ToList())
                    c.UserFields.Remove(field);
                c.UserFields.Add(new UserField { Key = key, Value = value.ToString(), StoreId = "" });
            })
            .Returns(Task.CompletedTask);
        _twoFactor = new Mock<ITwoFactorAuthenticationService>();
        _twoFactor.Setup(x => x.AuthenticateTwoFactor(It.IsAny<string>(), "123456", It.IsAny<Customer>(), Type))
            .ReturnsAsync(true);
        _resourceLock = new Mock<IResourceLock>();
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _mediator = new Mock<IMediator>();
        _settings = new CustomerSettings { FailedPasswordLockoutMinutes = 30 };
        _service = new TwoFactorSignInService(_twoFactor.Object, _customerService.Object, _resourceLock.Object,
            _mediator.Object, _settings);
    }

    //the request's own copy, loaded before the check: the service must not trust it
    private static Customer Loaded() => new() { Id = "c1" };

    private int WrongCodes() => _stored.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.TwoFactorWrongCodes);

    [TestMethod]
    public async Task RightCode_Passes()
    {
        Assert.AreEqual(TwoFactorSignInResult.Passed, await _service.CheckCode(Loaded(), "123456", Type));
    }

    [TestMethod]
    public async Task WrongCode_CountedOnTheCustomer_AndReportedAsAFailedLogin()
    {
        Assert.AreEqual(TwoFactorSignInResult.WrongCode, await _service.CheckCode(Loaded(), "000000", Type));

        Assert.AreEqual(1, WrongCodes());
        _mediator.Verify(x => x.Publish(It.Is<CustomerLoginFailedEvent>(e => e.Customer == _stored),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task LastWrongCodeAllowed_LocksTheCustomerOut()
    {
        for (var i = 1; i < TwoFactorSignInService.MaxWrongCodes; i++)
            Assert.AreEqual(TwoFactorSignInResult.WrongCode, await _service.CheckCode(Loaded(), "000000", Type));

        Assert.AreEqual(TwoFactorSignInResult.LockedOut, await _service.CheckCode(Loaded(), "000000", Type));

        Assert.IsTrue(_stored.CannotLoginUntilDateUtc > DateTime.UtcNow.AddMinutes(29));
        _customerService.Verify(x => x.UpdateCustomerLastLoginDate(_stored), Times.Once);
        Assert.AreEqual(0, WrongCodes());
    }

    [TestMethod]
    public async Task LockedOutCustomer_RefusedEvenWithTheRightCode()
    {
        _stored.CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(5);

        Assert.AreEqual(TwoFactorSignInResult.LockedOut, await _service.CheckCode(Loaded(), "123456", Type));
        _twoFactor.Verify(x => x.AuthenticateTwoFactor(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Customer>(), It.IsAny<TwoFactorAuthenticationType>()), Times.Never);
    }

    [TestMethod]
    public async Task CountSurvivesANewPasswordStep_OnlyAPassingCodeClearsIt()
    {
        //wrong codes of an earlier sign-in, kept on the customer
        _stored.UserFields.Add(new UserField {
            Key = SystemCustomerFieldNames.TwoFactorWrongCodes, Value = "3", StoreId = ""
        });

        Assert.AreEqual(TwoFactorSignInResult.WrongCode, await _service.CheckCode(Loaded(), "000000", Type));
        Assert.AreEqual(4, WrongCodes());

        Assert.AreEqual(TwoFactorSignInResult.Passed, await _service.CheckCode(Loaded(), "123456", Type));
        Assert.AreEqual(0, WrongCodes());
    }

    [TestMethod]
    public async Task NoLockoutLengthSet_StillLocksOut()
    {
        _settings.FailedPasswordLockoutMinutes = 0;

        for (var i = 0; i < TwoFactorSignInService.MaxWrongCodes; i++)
            await _service.CheckCode(Loaded(), "000000", Type);

        Assert.IsTrue(_stored.CannotLoginUntilDateUtc > DateTime.UtcNow);
    }

    [TestMethod]
    public async Task CheckedUnderTheCustomersLock()
    {
        await _service.CheckCode(Loaded(), "123456", Type);

        _resourceLock.Verify(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Single() == "TwoFactorSignIn:c1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
