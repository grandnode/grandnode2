using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Validators.Customers;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Admin.Tests.Validators;

/// <summary>
///     The owner field of the admin customer form: sub-accounts stay one level deep, and the owner already
///     linked never blocks an unrelated save.
/// </summary>
[TestClass]
public class CustomerValidatorOwnerTests
{
    private const string NotFound = "Admin.Customers.Customers.Fields.Owner.NotFound";
    private const string IsSubAccount = "Admin.Customers.Customers.Fields.Owner.IsSubAccount";
    private const string HasSubAccounts = "Admin.Customers.Customers.Fields.Owner.HasSubAccounts";
    private const string NotRegistered = "Admin.Customers.Customers.Fields.Owner.NotRegistered";
    private const string Self = "Admin.Customers.Customers.Fields.Owner.Self";

    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groupService;
    private CustomerConfig _customerConfig;
    private Mock<IAdminDataScope<Customer>> _scope;

    [TestInitialize]
    public void Setup()
    {
        //Admin panel: no default store, the owner field is editable
        _scope = new Mock<IAdminDataScope<Customer>>();
        _scope.Setup(s => s.GetDefaultStoreIdAsync()).ReturnsAsync((string)null);

        _groupService = new Mock<IGroupService>();
        _groupService
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup>());
        _groupService.Setup(g => g.IsAdmin(It.IsAny<Customer>())).ReturnsAsync(true);
        _groupService.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);

        //nobody has sub-accounts unless a test says so
        _customerService = new Mock<ICustomerService>();
        _customerService
            .Setup(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Customer, object>>>()))
            .ReturnsAsync(new PagedList<Customer>());
        _customerConfig = new CustomerConfig();
    }

    private CustomerValidator Validator()
    {
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(new Customer { Id = "admin" });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        return new CustomerValidator(new List<IValidatorConsumer<CustomerModel>>(), translation.Object,
            new Mock<ICountryService>().Object, context.Object, _customerService.Object, _groupService.Object,
            new CustomerSettings(), _customerConfig, _scope.Object);
    }

    private void SubAccountsOf(string ownerId, int count)
    {
        var list = new PagedList<Customer>();
        for (var i = 0; i < count; i++) list.Add(new Customer { OwnerId = ownerId });
        _customerService
            .Setup(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), ownerId, It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Customer, object>>>()))
            .ReturnsAsync(list);
    }

    private Customer Existing(string id = "c1", string ownerId = "")
    {
        var customer = new Customer { Id = id, Email = $"{id}@firm.com", OwnerId = ownerId };
        _customerService.Setup(c => c.GetCustomerById(id)).ReturnsAsync(customer);
        return customer;
    }

    private Customer OwnerAccount(string email = "boss@firm.com", string id = "owner-1", string ownerId = "")
    {
        var owner = new Customer { Id = id, Email = email, OwnerId = ownerId };
        _customerService.Setup(c => c.GetCustomerByEmail(email, It.IsAny<string>())).ReturnsAsync(owner);
        _customerService.Setup(c => c.GetCustomerById(id)).ReturnsAsync(owner);
        return owner;
    }

    private async Task<string[]> OwnerErrors(CustomerModel model) =>
        (await Validator().ValidateAsync(model)).Errors
        .Where(e => e.PropertyName == nameof(CustomerModel.Owner)).Select(e => e.ErrorMessage).ToArray();

    private static CustomerModel Edit(string owner, string id = "c1") =>
        new() { Id = id, Email = $"{id}@firm.com", StoreId = "s1", Owner = owner };

    private static CustomerModel Create(string owner) =>
        new() { Email = "new@firm.com", StoreId = "s1", Owner = owner };

    [TestMethod]
    public async Task ValidOwner_Passes()
    {
        Existing();
        OwnerAccount();

        CollectionAssert.AreEqual(Array.Empty<string>(), await OwnerErrors(Edit("boss@firm.com")));
        CollectionAssert.AreEqual(Array.Empty<string>(), await OwnerErrors(Create("boss@firm.com")));
    }

    [TestMethod]
    public async Task NoOwner_Passes()
    {
        Existing();

        CollectionAssert.AreEqual(Array.Empty<string>(), await OwnerErrors(Edit("")));
    }

    [TestMethod]
    public async Task UnknownOwner_Fails()
    {
        Existing();

        CollectionAssert.AreEqual(new[] { NotFound }, await OwnerErrors(Edit("nobody@firm.com")));
        CollectionAssert.AreEqual(new[] { NotFound }, await OwnerErrors(Create("nobody@firm.com")));
    }

    [TestMethod]
    public async Task DeletedOwner_Fails()
    {
        Existing();
        OwnerAccount().Deleted = true;

        CollectionAssert.AreEqual(new[] { NotFound }, await OwnerErrors(Edit("boss@firm.com")));
    }

    [TestMethod]
    public async Task OwnerThatIsASubAccount_Fails()
    {
        //a chain would let the top account reach orders two levels down
        Existing();
        OwnerAccount(ownerId: "top-owner");

        CollectionAssert.AreEqual(new[] { IsSubAccount }, await OwnerErrors(Edit("boss@firm.com")));
    }

    [TestMethod]
    public async Task CustomerWithSubAccounts_CannotGetAnOwner()
    {
        Existing();
        OwnerAccount();
        SubAccountsOf("c1", 2);

        CollectionAssert.AreEqual(new[] { HasSubAccounts }, await OwnerErrors(Edit("boss@firm.com")));
    }

    [TestMethod]
    public async Task UnregisteredOwner_Fails()
    {
        Existing();
        var owner = OwnerAccount();
        _groupService.Setup(g => g.IsRegistered(owner)).ReturnsAsync(false);

        CollectionAssert.AreEqual(new[] { NotRegistered }, await OwnerErrors(Edit("boss@firm.com")));
    }

    [TestMethod]
    public async Task OwnEmail_Fails()
    {
        Existing();

        CollectionAssert.AreEqual(new[] { Self }, await OwnerErrors(Edit("C1@Firm.com")));
    }

    [TestMethod]
    public async Task OwnAccountUnderAnotherEmail_Fails()
    {
        //the email lookup resolves to the customer being edited (e.g. its email is being changed at the same time)
        Existing();
        _customerService.Setup(c => c.GetCustomerByEmail("old@firm.com", It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "c1" });

        CollectionAssert.AreEqual(new[] { Self }, await OwnerErrors(Edit("old@firm.com")));
    }

    [TestMethod]
    public async Task OwnerAlreadyLinked_NeverBlocksTheSave()
    {
        //legacy data the rules would now refuse (the owner became a sub-account) must not lock the form
        Existing(ownerId: "owner-1");
        OwnerAccount(ownerId: "top-owner");

        CollectionAssert.AreEqual(Array.Empty<string>(), await OwnerErrors(Edit("Boss@Firm.com")));
    }

    [TestMethod]
    public async Task StorePanel_DoesNotCheckThePostedOwner()
    {
        //the Store panel replaces the posted owner before saving; checking it would answer a store manager
        //whether another store's e-mail exists, is a sub-account, is registered or has sub-accounts
        _scope.Setup(s => s.GetDefaultStoreIdAsync()).ReturnsAsync("s1");
        Existing();
        OwnerAccount(ownerId: "top-owner");

        CollectionAssert.AreEqual(Array.Empty<string>(), await OwnerErrors(Edit("boss@firm.com")));
        _customerService.Verify(c => c.GetCustomerByEmail("boss@firm.com", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OwnerAlreadyLinked_CustomerMovedToAnotherStore_MustHoldInTheNewStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        var customer = Existing(ownerId: "owner-1");
        customer.StoreId = "store-a";
        var owner = new Customer { Id = "owner-1", Email = "boss@firm.com" };
        _customerService.Setup(c => c.GetCustomerById("owner-1")).ReturnsAsync(owner);
        _customerService.Setup(c => c.GetCustomerByEmail("boss@firm.com", "store-a")).ReturnsAsync(owner);

        //Edit() posts StoreId "s1": the owner lives in store-a only, so the link would cross stores
        CollectionAssert.AreEqual(new[] { NotFound }, await OwnerErrors(Edit("boss@firm.com")));
    }

    [TestMethod]
    public async Task PerStoreIdentity_LooksTheOwnerUpInTheCustomersStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        Existing();
        OwnerAccount();

        await OwnerErrors(Edit("boss@firm.com"));

        _customerService.Verify(c => c.GetCustomerByEmail("boss@firm.com", "s1"), Times.Once);
    }
}
