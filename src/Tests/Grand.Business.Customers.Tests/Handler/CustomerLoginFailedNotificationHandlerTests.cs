using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Customers.Events.Handlers;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Handler;

[TestClass]
public class CustomerLoginFailedNotificationHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private CustomerSettings _customerSettings;
    private CustomerLoginFailedEventHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerSettings = new CustomerSettings { FailedPasswordAllowedAttempts = 10 };
        _handler = new CustomerLoginFailedEventHandler(_customerServiceMock.Object, _customerSettings);
    }

    [TestMethod]
    public async Task HandleTest_FailedLoginAttempts_Inc()
    {
        //Arrange
        var customer = new Customer {
            FailedLoginAttempts = 1,
            CannotLoginUntilDateUtc = null
        };
        //Act
        await _handler.Handle(new CustomerLoginFailedEvent(customer), CancellationToken.None);

        //Assert
        Assert.IsTrue(customer.FailedLoginAttempts == 2);
        _customerServiceMock.Verify(c => c.UpdateCustomerLastLoginDate(customer), Times.Once);
    }

    [TestMethod]
    public async Task HandleTest_CannotLoginUntilDate_Set()
    {
        //Arrange
        var customer = new Customer {
            FailedLoginAttempts = 10,
            CannotLoginUntilDateUtc = null
        };
        //Act
        await _handler.Handle(new CustomerLoginFailedEvent(customer), CancellationToken.None);

        //Assert
        Assert.IsTrue(customer.CannotLoginUntilDateUtc != null);
        _customerServiceMock.Verify(c => c.UpdateCustomerLastLoginDate(customer), Times.Once);
    }
}