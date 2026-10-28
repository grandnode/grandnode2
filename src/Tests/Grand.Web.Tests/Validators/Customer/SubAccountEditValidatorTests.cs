using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Localization;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Customers;

[TestClass]
public class SubAccountEditValidatorTests
{
    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groupService;
    private SubAccountEditValidator _validator;

    [TestInitialize]
    public void Init()
    {
        _customerService = new Mock<ICustomerService>();
        _groupService = new Mock<IGroupService>();
        _groupService.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);

        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(new Customer { Id = "owner" });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        _validator = new SubAccountEditValidator(new List<IValidatorConsumer<SubAccountEditModel>>(),
            _customerService.Object, _groupService.Object, translation.Object, context.Object,
            new CustomerSettings { AllowUsersToChangeEmail = true }, new CustomerConfig());
    }

    private static SubAccountEditModel Model(string email = "new@x.com") =>
        new() { Id = "c1", Email = email, FirstName = "A", LastName = "B" };

    [TestMethod]
    public async Task SomeoneElsesAccount_GetsOnlyTheNotOwnerAnswer()
    {
        //an unregistered account of another owner: its state must not show through
        var victim = new Customer { Id = "c1", OwnerId = "another-owner", Email = "victim@x.com" };
        _customerService.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(victim);
        _groupService.Setup(g => g.IsRegistered(victim)).ReturnsAsync(false);

        var result = await _validator.ValidateAsync(Model());

        CollectionAssert.AreEqual(new[] { "Account.SubAccounts.NotOwner" },
            result.Errors.Select(e => e.ErrorMessage).ToArray());
        //and no probing of whether the new email is taken
        _customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task MissingAccount_GetsTheSameAnswerAsSomeoneElses()
    {
        //otherwise the answer tells which customer ids exist
        var result = await _validator.ValidateAsync(Model());

        CollectionAssert.AreEqual(new[] { "Account.SubAccounts.NotOwner" },
            result.Errors.Select(e => e.ErrorMessage).ToArray());
    }

    [TestMethod]
    public async Task OwnDeletedSubAccount_GetsTheSameAnswer()
    {
        //the edit page and the command treat it as not yours; "Delete account" was a button label, not an error
        _customerService.Setup(c => c.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { Id = "c1", OwnerId = "owner", Deleted = true, Email = "d@x.com" });

        var result = await _validator.ValidateAsync(Model());

        CollectionAssert.AreEqual(new[] { "Account.SubAccounts.NotOwner" },
            result.Errors.Select(e => e.ErrorMessage).ToArray());
    }

    [TestMethod]
    public async Task NotRegisteredSubAccount_UsesTheResourceNotARawKey()
    {
        var sub = new Customer { Id = "c1", OwnerId = "owner", Email = "sub@x.com" };
        _customerService.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(sub);
        _groupService.Setup(g => g.IsRegistered(sub)).ReturnsAsync(false);

        var result = await _validator.ValidateAsync(Model());

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Account.Login.WrongCredentials.NotRegistered"));
    }

    [TestMethod]
    public async Task EmptyEmail_FailsValidationWithoutThrowing()
    {
        _customerService.Setup(c => c.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { Id = "c1", OwnerId = "owner", Email = "sub@x.com" });

        var result = await _validator.ValidateAsync(Model(email: null));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Account.Fields.Email.Required"));
    }

    [TestMethod]
    public async Task SameEmailInOtherCase_IsNotAnEmailChange()
    {
        _customerService.Setup(c => c.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { Id = "c1", OwnerId = "owner", Email = "sub@x.com" });

        var result = await _validator.ValidateAsync(Model(email: "Sub@X.com"));

        Assert.IsTrue(result.IsValid);
        _customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task NewEmailTakenByAnotherCustomer_Fails()
    {
        _customerService.Setup(c => c.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { Id = "c1", OwnerId = "owner", Email = "sub@x.com" });
        _customerService.Setup(c => c.GetCustomerByEmail("new@x.com", It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "other" });

        var result = await _validator.ValidateAsync(Model());

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Account.EmailUsernameErrors.EmailAlreadyExists"));
    }
}
