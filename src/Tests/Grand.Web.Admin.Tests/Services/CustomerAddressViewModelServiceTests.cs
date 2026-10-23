using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerAddressViewModelServiceTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private CustomerAddressViewModelService _customerAddressViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerServiceMock.Setup(c => c.UpdateCustomerInAdminPanel(It.IsAny<Customer>())).Returns(Task.CompletedTask);

        _customerAddressViewModelService = new CustomerAddressViewModelService(
            _customerServiceMock.Object,
            new Mock<ICountryService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IContextAccessor>().Object,
            new AddressSettings());
    }

    [TestMethod]
    public async Task DeleteAddress_RemovesAddressAndUpdatesCustomer()
    {
        var address = new Address { Id = "a1" };
        var customer = new Customer();
        customer.Addresses.Add(address);

        await _customerAddressViewModelService.DeleteAddress(customer, address);

        Assert.IsFalse(customer.Addresses.Any(a => a.Id == "a1"));
        _customerServiceMock.Verify(c => c.UpdateCustomerInAdminPanel(customer), Times.Once);
    }
}
