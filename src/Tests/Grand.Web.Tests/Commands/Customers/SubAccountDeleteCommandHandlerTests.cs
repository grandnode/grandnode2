using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Handler.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Customers;

[TestClass]
public class SubAccountDeleteCommandHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private SubAccountDeleteCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(key => key);
        _handler = new SubAccountDeleteCommandHandler(_customerServiceMock.Object, translation.Object);
    }

    private static SubAccountDeleteCommand Command(string id) =>
        new() { CurrentCustomer = new Customer { Id = "owner" }, CustomerId = id };

    [TestMethod]
    public async Task Handle_OwnSubAccount_DeletesIt()
    {
        var subAccount = new Customer { Id = "sub", OwnerId = "owner" };
        _customerServiceMock.Setup(c => c.GetCustomerById("sub")).ReturnsAsync(subAccount);

        var (success, error) = await _handler.Handle(Command("sub"), default);

        Assert.IsTrue(success);
        Assert.AreEqual(string.Empty, error);
        _customerServiceMock.Verify(c => c.DeleteCustomer(subAccount, false), Times.Once);
    }

    [TestMethod]
    [DataRow("another-owner")]
    [DataRow("")]
    public async Task Handle_SomeoneElsesAccount_DeletesNothing(string ownerId)
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("victim"))
            .ReturnsAsync(new Customer { Id = "victim", OwnerId = ownerId });

        var (success, error) = await _handler.Handle(Command("victim"), default);

        Assert.IsFalse(success);
        Assert.AreEqual("Account.SubAccounts.NotOwner", error);
        _customerServiceMock.Verify(c => c.DeleteCustomer(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_MissingOrDeletedAccount_GivesTheSameAnswer()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("gone"))
            .ReturnsAsync(new Customer { Id = "gone", OwnerId = "owner", Deleted = true });

        var deleted = await _handler.Handle(Command("gone"), default);
        var missing = await _handler.Handle(Command("missing"), default);

        Assert.AreEqual(missing, deleted);
        Assert.IsFalse(deleted.success);
        _customerServiceMock.Verify(c => c.DeleteCustomer(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }
}
