using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Handler.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Customers;

[TestClass]
public class SubAccountEditCommandHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private SubAccountEditCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _handler = new SubAccountEditCommandHandler(_customerServiceMock.Object,
            _customerManagerServiceMock.Object, new CustomerSettings { AllowUsersToChangeEmail = true });
    }

    [TestMethod]
    public async Task Handle_WithPassword_ChangesPasswordScopedToCustomerStore()
    {
        var customer = new Customer { Id = "c1", Email = "sub@x.com", StoreId = "store-9" };
        _customerServiceMock.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(customer);

        var command = new SubAccountEditCommand {
            CurrentCustomer = new Customer { Id = "owner" },
            EditModel = new SubAccountEditModel {
                Id = "c1", Email = "sub@x.com", Password = "newpass", Active = true,
                FirstName = "John", LastName = "Doe"
            }
        };

        var result = await _handler.Handle(command, default);

        Assert.IsTrue(result);
        //the sub-account's store must scope the password change (safe with or without per-store identity)
        _customerManagerServiceMock.Verify(m => m.ChangePassword(
            It.Is<ChangePasswordRequest>(r => r.Email == "sub@x.com"), "store-9"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutPassword_DoesNotChangePassword()
    {
        var customer = new Customer { Id = "c1", Email = "sub@x.com", StoreId = "store-9" };
        _customerServiceMock.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(customer);

        var command = new SubAccountEditCommand {
            CurrentCustomer = new Customer { Id = "owner" },
            EditModel = new SubAccountEditModel { Id = "c1", Email = "sub@x.com", Active = true }
        };

        var result = await _handler.Handle(command, default);

        Assert.IsTrue(result);
        _customerManagerServiceMock.Verify(m => m.ChangePassword(
            It.IsAny<ChangePasswordRequest>(), It.IsAny<string>()), Times.Never);
    }
}
