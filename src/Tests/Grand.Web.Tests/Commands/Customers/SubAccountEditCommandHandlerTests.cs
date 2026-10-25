using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Handler.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Globalization;
using System.Linq.Expressions;

namespace Grand.Web.Tests.Commands.Customers;

[TestClass]
public class SubAccountEditCommandHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private CustomerSettings _customerSettings;
    private SubAccountEditCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        _customerSettings = new CustomerSettings { AllowUsersToChangeEmail = true };
        _handler = new SubAccountEditCommandHandler(_customerServiceMock.Object,
            _customerManagerServiceMock.Object, _customerSettings);
    }

    private CultureInfo _culture;

    [TestCleanup]
    public void Restore() => CultureInfo.CurrentCulture = _culture;

    private Customer SubAccount(string ownerId = "owner")
    {
        var customer = new Customer {
            Id = "c1", Email = "sub@x.com", Username = "sub@x.com", StoreId = "store-9", OwnerId = ownerId
        };
        _customerServiceMock.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(customer);
        return customer;
    }

    private static SubAccountEditCommand Command(string email = "sub@x.com", string password = null,
        SubAccountRole role = SubAccountRole.Buyer, string limit = null) => new() {
        CurrentCustomer = new Customer { Id = "owner" },
        EditModel = new SubAccountEditModel {
            Id = "c1", Email = email, Password = password, Active = true, FirstName = "John", LastName = "Doe",
            Role = role, OrderApprovalLimit = limit
        }
    };

    private void VerifyNothingWritten()
    {
        _customerServiceMock.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()), Times.Never);
        _customerServiceMock.Verify(c => c.UpdateActive(It.IsAny<Customer>()), Times.Never);
        _customerServiceMock.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _customerManagerServiceMock.Verify(m => m.ChangePassword(It.IsAny<ChangePasswordRequest>(),
            It.IsAny<string>()), Times.Never);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, SubAccountRole>>>(), It.IsAny<SubAccountRole>()), Times.Never);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, double?>>>(), It.IsAny<double?>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_SetsRoleAndLimit()
    {
        var customer = SubAccount();

        var result = await _handler.Handle(Command(role: SubAccountRole.Buyer, limit: "250.5"), default);

        Assert.IsTrue(result);
        Assert.AreEqual(SubAccountRole.Buyer, customer.SubAccountRole);
        Assert.AreEqual(250.5, customer.OrderApprovalLimit);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(customer,
            It.IsAny<Expression<Func<Customer, SubAccountRole>>>(), SubAccountRole.Buyer), Times.Once);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(customer,
            It.IsAny<Expression<Func<Customer, double?>>>(), (double?)250.5), Times.Once);
    }

    [TestMethod]
    [DataRow("pl-PL", "250,5", 250.5)]
    [DataRow("de-DE", "1.500,50", 1500.5)]
    [DataRow("en-US", "250.5", 250.5)]
    [DataRow("pl-PL", "", null)]
    [DataRow("pl-PL", "  ", null)]
    public async Task Handle_ReadsTheLimitInTheCurrentCulture(string culture, string typed, double? expected)
    {
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        var customer = SubAccount();

        await _handler.Handle(Command(limit: typed), default);

        Assert.AreEqual(expected, customer.OrderApprovalLimit);
    }

    [TestMethod]
    public async Task Handle_BuyerWithoutLimit_ClearsAPreviousLimit()
    {
        var customer = SubAccount();
        customer.OrderApprovalLimit = 100;

        await _handler.Handle(Command(role: SubAccountRole.Buyer, limit: null), default);

        Assert.IsNull(customer.OrderApprovalLimit);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(customer,
            It.IsAny<Expression<Func<Customer, double?>>>(), (double?)null), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Approver_ClearsLimit()
    {
        var customer = SubAccount();
        customer.OrderApprovalLimit = 100;

        await _handler.Handle(Command(role: SubAccountRole.Approver, limit: "500"), default);

        Assert.AreEqual(SubAccountRole.Approver, customer.SubAccountRole);
        Assert.IsNull(customer.OrderApprovalLimit);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(customer,
            It.IsAny<Expression<Func<Customer, double?>>>(), (double?)null), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithPassword_ChangesPasswordScopedToCustomerStore()
    {
        SubAccount();

        var result = await _handler.Handle(Command(password: "newpass"), default);

        Assert.IsTrue(result);
        //the sub-account's store must scope the password change (safe with or without per-store identity)
        _customerManagerServiceMock.Verify(m => m.ChangePassword(
            It.Is<ChangePasswordRequest>(r => r.Email == "sub@x.com"), "store-9"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutPassword_DoesNotChangePassword()
    {
        SubAccount();

        var result = await _handler.Handle(Command(), default);

        Assert.IsTrue(result);
        _customerManagerServiceMock.Verify(m => m.ChangePassword(
            It.IsAny<ChangePasswordRequest>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_SomeoneElsesAccount_ChangesNothing()
    {
        SubAccount(ownerId: "another-owner");

        var result = await _handler.Handle(Command(email: "taken@x.com", password: "newpass"), default);

        Assert.IsFalse(result);
        VerifyNothingWritten();
    }

    [TestMethod]
    public async Task Handle_AccountWithoutOwner_ChangesNothing()
    {
        //any main account's id, the current owner's own included: it has no owner, so it is nobody's sub-account
        SubAccount(ownerId: "");

        var result = await _handler.Handle(Command(), default);

        Assert.IsFalse(result);
        VerifyNothingWritten();
    }

    [TestMethod]
    public async Task Handle_DeletedAccount_ChangesNothing()
    {
        SubAccount().Deleted = true;

        var result = await _handler.Handle(Command(), default);

        Assert.IsFalse(result);
        VerifyNothingWritten();
    }

    [TestMethod]
    public async Task Handle_MissingAccount_ChangesNothing()
    {
        var result = await _handler.Handle(Command(), default);

        Assert.IsFalse(result);
        VerifyNothingWritten();
    }

    [TestMethod]
    public async Task Handle_MixedCaseEmail_IsStoredLowercased()
    {
        //stored as typed, the lowercased login lookup missed it and the sub-account could no longer sign in
        var customer = SubAccount();

        await _handler.Handle(Command(email: "  New.Sub@X.com "), default);

        Assert.AreEqual("new.sub@x.com", customer.Email);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(customer,
            It.IsAny<Expression<Func<Customer, string>>>(), "new.sub@x.com"), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Handle_NewEmailAndPassword_ChangesPasswordOfTheNewEmail()
    {
        SubAccount();

        await _handler.Handle(Command(email: "New@X.com", password: "newpass"), default);

        _customerManagerServiceMock.Verify(m => m.ChangePassword(
            It.Is<ChangePasswordRequest>(r => r.Email == "new@x.com"), "store-9"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NewEmail_UsernamesDisabled_KeepsUsernameEqualToEmail()
    {
        var customer = SubAccount();

        await _handler.Handle(Command(email: "new@x.com"), default);

        Assert.AreEqual("new@x.com", customer.Username);
    }

    [TestMethod]
    public async Task Handle_NewEmail_UsernamesEnabled_LeavesUsername()
    {
        _customerSettings.UsernamesEnabled = true;
        var customer = SubAccount();

        await _handler.Handle(Command(email: "new@x.com"), default);

        Assert.AreEqual("new@x.com", customer.Email);
        Assert.AreEqual("sub@x.com", customer.Username);
    }

    [TestMethod]
    public async Task Handle_EmailStoredInMixedCaseByOlderEdits_IsRepairedEvenWhenChangesAreNotAllowed()
    {
        //such a row cannot sign in, and its password change looked it up by that e-mail and threw
        _customerSettings.AllowUsersToChangeEmail = false;
        var customer = SubAccount();
        customer.Email = "Sub@X.com";

        await _handler.Handle(Command(email: "Sub@X.com", password: "newpass"), default);

        Assert.AreEqual("sub@x.com", customer.Email);
        _customerManagerServiceMock.Verify(m => m.ChangePassword(
            It.Is<ChangePasswordRequest>(r => r.Email == "sub@x.com"), "store-9"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_EmailChangeNotAllowed_KeepsEmail()
    {
        _customerSettings.AllowUsersToChangeEmail = false;
        var customer = SubAccount();

        await _handler.Handle(Command(email: "new@x.com"), default);

        Assert.AreEqual("sub@x.com", customer.Email);
    }
}
