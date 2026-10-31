using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Validators.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class CustomerValidatorTests
{
    private const string CurrentStoreId = "store-current";
    private const string RequiredMessage = "Admin.Customers.Customers.Fields.Store.Required";
    private const string MustBeCurrentStoreMessage = "Admin.Customers.Customers.Fields.Store.MustBeCurrentStore";

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private CustomerValidator _validator;

    [TestInitialize]
    public void Setup()
    {
        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);

        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _groupServiceMock
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup>());

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer());
        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Store { Id = CurrentStoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        _validator = new CustomerValidator(
            new List<IValidatorConsumer<CustomerModel>>(),
            translationServiceMock.Object,
            new Mock<ICountryService>().Object,
            contextAccessorMock.Object,
            _customerServiceMock.Object,
            _groupServiceMock.Object,
            new CustomerSettings(),
            new CustomerConfig(),
            new Mock<IAdminDataScope<Customer>>().Object);
    }

    private static CustomerModel BuildModel(string storeId) =>
        new() { Id = "customer-1", Email = "customer@example.com", StoreId = storeId };

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Email_Empty_GuestWithoutEmail_ReportsRequired(string email)
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("customer-1"))
            .ReturnsAsync(new Customer { Id = "customer-1", Email = null });

        var result = await _validator.ValidateAsync(new CustomerModel { Id = "customer-1", Email = email, StoreId = CurrentStoreId });

        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(CustomerModel.Email)));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Email_Cleared_RegisteredCustomer_ReportsRequired(string email)
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("customer-1"))
            .ReturnsAsync(new Customer { Id = "customer-1", Email = "customer@example.com" });

        var result = await _validator.ValidateAsync(new CustomerModel { Id = "customer-1", Email = email, StoreId = CurrentStoreId });

        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(CustomerModel.Email)));
    }

    [TestMethod]
    public async Task Email_Changed_IsStillCheckedForDuplicates()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("customer-1"))
            .ReturnsAsync(new Customer { Id = "customer-1", Email = "customer@example.com" });
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("taken@example.com", It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "customer-2", Email = "taken@example.com" });

        var result = await _validator.ValidateAsync(new CustomerModel { Id = "customer-1", Email = "taken@example.com", StoreId = CurrentStoreId });

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Account.EmailUsernameErrors.EmailAlreadyExists"));
    }

    [TestMethod]
    public async Task StoreId_Empty_FailsRequired()
    {
        var result = await _validator.ValidateAsync(BuildModel(string.Empty));

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == RequiredMessage));
    }

    [TestMethod]
    public async Task StoreId_Empty_Admin_DoesNotFailRequired()
    {
        //an administrator may manage a store-independent (storeless) account
        _groupServiceMock.Setup(g => g.IsAdmin(It.IsAny<Customer>())).ReturnsAsync(true);

        var result = await _validator.ValidateAsync(BuildModel(string.Empty));

        Assert.IsFalse(result.Errors.Any(e => e.ErrorMessage == RequiredMessage));
    }

    [TestMethod]
    public async Task StoreManager_WrongStore_Fails()
    {
        _groupServiceMock.Setup(g => g.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(true);

        var result = await _validator.ValidateAsync(BuildModel("store-other"));

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == MustBeCurrentStoreMessage));
    }

    [TestMethod]
    public async Task StoreManager_CurrentStore_Passes()
    {
        _groupServiceMock.Setup(g => g.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(true);

        var result = await _validator.ValidateAsync(BuildModel(CurrentStoreId));

        Assert.IsFalse(result.Errors.Any(e => e.ErrorMessage == MustBeCurrentStoreMessage));
    }

    [TestMethod]
    public async Task NonStoreManager_AnyStore_Passes()
    {
        _groupServiceMock.Setup(g => g.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(false);

        var result = await _validator.ValidateAsync(BuildModel("store-other"));

        Assert.IsFalse(result.Errors.Any(e => e.ErrorMessage == MustBeCurrentStoreMessage));
    }
}
