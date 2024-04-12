using Grand.Business.Core.Commands.Customers;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Customers.Commands.Handlers;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Handler;

[TestClass]
public class ActiveVendorCommandHandlerTests
{
    private Mock<ICustomerService> _cumstomerServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private ActiveVendorCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IVendorService> _vendorMock;

    [TestInitialize]
    public void Init()
    {
        _vendorMock = new Mock<IVendorService>();
        _cumstomerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _mediatorMock = new Mock<IMediator>();
        _handler = new ActiveVendorCommandHandler(_vendorMock.Object, _cumstomerServiceMock.Object,
            _groupServiceMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task Handle_ValidArguments_InsertCustomerGroupInCustomer()
    {
        var command = new ActiveVendorCommand {
            CustomerIds = new List<string> { "id" },
            Active = true,
            Vendor = new Vendor()
        };
        _cumstomerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = true }));
        _groupServiceMock.Setup(c => c.IsAdmin(It.IsAny<Customer>())).Returns(() => Task.FromResult(false));
        _groupServiceMock.Setup(c => c.GetCustomerGroupBySystemName(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new CustomerGroup()));
        await _handler.Handle(command, default);
        _cumstomerServiceMock.Verify(
            c => c.InsertCustomerGroupInCustomer(It.IsAny<CustomerGroup>(), It.IsAny<string>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<VendorActivationEvent>(), default));
    }


    [TestMethod]
    public async Task Handle_ValidArguments_DeleteCustomerGroupInCustomer()
    {
        var command = new ActiveVendorCommand {
            CustomerIds = new List<string> { "id" },
            Active = false,
            Vendor = new Vendor()
        };
        _cumstomerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = true }));
        _groupServiceMock.Setup(c => c.IsAdmin(It.IsAny<Customer>())).Returns(() => Task.FromResult(false));
        _groupServiceMock.Setup(c => c.IsVendor(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
        _groupServiceMock.Setup(c => c.GetCustomerGroupBySystemName(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new CustomerGroup()));
        await _handler.Handle(command, default);
        _cumstomerServiceMock.Verify(
            c => c.DeleteCustomerGroupInCustomer(It.IsAny<CustomerGroup>(), It.IsAny<string>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<VendorActivationEvent>(), default));
    }
}