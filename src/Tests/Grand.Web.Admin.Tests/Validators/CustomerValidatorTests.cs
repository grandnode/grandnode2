using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Validators.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class CustomerValidatorTests
{
    private const string CurrentStoreId = "store-current";
    private const string RequiredMessage = "Admin.Customers.Customers.Fields.Store.Required";
    private const string MustBeCurrentStoreMessage = "Admin.Customers.Customers.Fields.Store.MustBeCurrentStore";

    private Mock<IGroupService> _groupServiceMock;
    private CustomerValidator _validator;

    [TestInitialize]
    public void Setup()
    {
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);

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
            new Mock<ICustomerService>().Object,
            _groupServiceMock.Object,
            new CustomerSettings());
    }

    private static CustomerModel BuildModel(string storeId) =>
        new() { Id = "customer-1", Email = "customer@example.com", StoreId = storeId };

    [TestMethod]
    public async Task StoreId_Empty_FailsRequired()
    {
        var result = await _validator.ValidateAsync(BuildModel(string.Empty));

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == RequiredMessage));
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
