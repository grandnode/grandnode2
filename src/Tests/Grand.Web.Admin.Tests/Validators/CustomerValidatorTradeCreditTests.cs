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
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class CustomerValidatorTradeCreditTests
{
    private const string BothRequired = "Admin.Customers.Customers.Fields.TradeCredit.BothRequired";
    private const string SubAccount = "Admin.Customers.Customers.Fields.TradeCredit.SubAccount";
    private const string LimitInvalid = "Admin.Customers.Customers.Fields.CreditLimit.Invalid";
    private const string TermInvalid = "Admin.Customers.Customers.Fields.PaymentTermDays.Invalid";

    private CustomerValidator _validator;

    [TestInitialize]
    public void Setup()
    {
        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup>());

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer());
        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Store { Id = "store-1" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        _validator = new CustomerValidator(
            new List<IValidatorConsumer<CustomerModel>>(),
            translationServiceMock.Object,
            new Mock<ICountryService>().Object,
            contextAccessorMock.Object,
            new Mock<ICustomerService>().Object,
            groupServiceMock.Object,
            new CustomerSettings(),
            new CustomerConfig(),
            new Mock<IAdminDataScope<Customer>>().Object);
    }

    private async Task<List<string>> Errors(double? limit, int? term, string owner = null)
    {
        var result = await _validator.ValidateAsync(new CustomerModel {
            Id = "customer-1", Email = "buyer@example.com", StoreId = "store-1",
            CreditLimit = limit, PaymentTermDays = term, Owner = owner
        });
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }

    [TestMethod]
    public async Task NegativeLimit_Error()
    {
        CollectionAssert.Contains(await Errors(-1, 30), LimitInvalid);
    }

    [TestMethod]
    public async Task TermOverAYear_Error()
    {
        CollectionAssert.Contains(await Errors(1000, 366), TermInvalid);
    }

    [TestMethod]
    public async Task NegativeTerm_Error()
    {
        CollectionAssert.Contains(await Errors(1000, -1), TermInvalid);
    }

    [TestMethod]
    public async Task LimitWithoutTerm_BothRequired()
    {
        CollectionAssert.Contains(await Errors(1000, null), BothRequired);
    }

    [TestMethod]
    public async Task TermWithoutLimit_BothRequired()
    {
        CollectionAssert.Contains(await Errors(null, 30), BothRequired);
    }

    [TestMethod]
    public async Task SubAccountWithCredit_Error()
    {
        CollectionAssert.Contains(await Errors(1000, 30, "boss@firm.com"), SubAccount);
    }

    [TestMethod]
    public async Task LimitAndTerm_Valid()
    {
        var errors = await Errors(1000, 30);

        CollectionAssert.DoesNotContain(errors, BothRequired);
        CollectionAssert.DoesNotContain(errors, LimitInvalid);
        CollectionAssert.DoesNotContain(errors, TermInvalid);
    }

    [TestMethod]
    public async Task BothEmpty_Valid()
    {
        var errors = await Errors(null, null, "boss@firm.com");

        CollectionAssert.DoesNotContain(errors, BothRequired);
        CollectionAssert.DoesNotContain(errors, SubAccount);
    }
}
