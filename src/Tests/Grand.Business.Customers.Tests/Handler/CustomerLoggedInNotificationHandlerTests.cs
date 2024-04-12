using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Customers.Events.Handlers;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Handler;

[TestClass]
public class CustomerLoggedInNotificationHandlerTests
{
    private Mock<ICustomerService> _cumstomerServiceMock;
    private CustomerLoggedInEventHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _cumstomerServiceMock = new Mock<ICustomerService>();
        _handler = new CustomerLoggedInEventHandler(_cumstomerServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var customer = new Customer {
            FailedLoginAttempts = 1,
            CannotLoginUntilDateUtc = DateTime.Now
        };
        //Act
        await _handler.Handle(new CustomerLoggedInEvent(customer), CancellationToken.None);

        //Assert
        Assert.IsTrue(customer.FailedLoginAttempts == 0);
        Assert.IsTrue(customer.CannotLoginUntilDateUtc == null);
        _cumstomerServiceMock.Verify(c => c.UpdateCustomerLastLoginDate(customer), Times.Once);
    }
}