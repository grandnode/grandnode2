using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.Commands.Handlers.Customers;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Customers;

[TestClass]
public class DeleteCustomerCommandHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
    }

    private DeleteCustomerCommandHandler CreateHandler(bool perStore) =>
        new(_customerServiceMock.Object, new CustomerConfig { RegisterCustomersPerStore = perStore });

    [TestMethod]
    public async Task Handle_CustomerNotFound_ReturnsTrue_AndDoesNotDelete()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Customer)null);
        var handler = CreateHandler(perStore: true);

        var result = await handler.Handle(new DeleteCustomerCommand { Email = "none@x.com" }, default);

        Assert.IsTrue(result);
        _customerServiceMock.Verify(c => c.DeleteCustomer(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_StoreCustomer_Deletes()
    {
        var customer = new Customer { Id = "c1", Email = "u@x.com", StoreId = "store-1" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(customer);
        var handler = CreateHandler(perStore: true);

        var result = await handler.Handle(new DeleteCustomerCommand { Email = "u@x.com" }, default);

        Assert.IsTrue(result);
        _customerServiceMock.Verify(c => c.DeleteCustomer(customer, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PerStoreOn_StorelessAccount_Refuses()
    {
        //the by-email lookup resolved a store-independent (admin/back-office) account
        var admin = new Customer { Id = "admin", Email = "admin@x.com", StoreId = "" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(admin);
        var handler = CreateHandler(perStore: true);

        await Assert.ThrowsExactlyAsync<GrandException>(() =>
            handler.Handle(new DeleteCustomerCommand { Email = "admin@x.com" }, default));

        _customerServiceMock.Verify(c => c.DeleteCustomer(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PerStoreOff_StorelessAccount_Deletes()
    {
        //with the flag off the guard does not apply (e-mail is globally unique)
        var customer = new Customer { Id = "c1", Email = "u@x.com", StoreId = "" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(customer);
        var handler = CreateHandler(perStore: false);

        var result = await handler.Handle(new DeleteCustomerCommand { Email = "u@x.com" }, default);

        Assert.IsTrue(result);
        _customerServiceMock.Verify(c => c.DeleteCustomer(customer, It.IsAny<bool>()), Times.Once);
    }
}
