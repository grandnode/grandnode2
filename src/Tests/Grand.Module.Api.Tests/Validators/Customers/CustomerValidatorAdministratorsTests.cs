using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Validators.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Validators.Customers;

[TestClass]
public class CustomerValidatorAdministratorsTests
{
    private const string Message = "Only administrators can assign role 'Administrators' or change an administrator";

    private readonly Customer _current = new() { Id = "staff" };
    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groupService;
    private CustomerValidator _validator;

    public CustomerValidatorAdministratorsTests()
    {
        _customerService = new Mock<ICustomerService>();
        _groupService = new Mock<IGroupService>();
        _groupService.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Administrators))
            .ReturnsAsync(new CustomerGroup { Id = "admins", SystemName = SystemCustomerGroupNames.Administrators });
        var storeService = new Mock<IStoreService>();
        storeService.Setup(s => s.GetStoreById(It.IsAny<string>())).ReturnsAsync(new Store());
        var contextAccessor = new Mock<IContextAccessor> { DefaultValue = DefaultValue.Mock };
        contextAccessor.Setup(c => c.WorkContext.CurrentCustomer).Returns(_current);
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(t => t.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>())).Returns<string, string, string, bool>((key, _, _, _) => key);

        _validator = new CustomerValidator(new List<IValidatorConsumer<CustomerDto>>(),
            translationService.Object, contextAccessor.Object, Mock.Of<ICountryService>(),
            _customerService.Object, storeService.Object, new CustomerSettings(), _groupService.Object);
    }

    private async Task<bool> Refused(CustomerDto dto) =>
        (await _validator.ValidateAsync(dto)).Errors.Any(e => e.ErrorMessage == Message);

    [TestMethod]
    public async Task NonAdministrator_GivesTheAdministratorRole_Refused()
    {
        Assert.IsTrue(await Refused(new CustomerDto { Groups = ["admins"] }));
    }

    [TestMethod]
    public async Task NonAdministrator_ChangesAnAdministrator_Refused()
    {
        var admin = new Customer { Id = "c1" };
        _customerService.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(admin);
        _groupService.Setup(g => g.IsAdmin(admin)).ReturnsAsync(true);

        Assert.IsTrue(await Refused(new CustomerDto { Id = "c1", Groups = ["registered"] }));
    }

    [TestMethod]
    public async Task NonAdministrator_ChangesAnotherCustomer_Allowed()
    {
        _customerService.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1" });

        Assert.IsFalse(await Refused(new CustomerDto { Id = "c1", Groups = ["registered"] }));
    }

    [TestMethod]
    public async Task Administrator_GivesTheAdministratorRole_Allowed()
    {
        _groupService.Setup(g => g.IsAdmin(_current)).ReturnsAsync(true);

        Assert.IsFalse(await Refused(new CustomerDto { Groups = ["admins"] }));
    }
}
