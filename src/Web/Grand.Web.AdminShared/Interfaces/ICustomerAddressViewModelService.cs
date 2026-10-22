using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerAddressViewModelService
{
    Task<IEnumerable<AddressModel>> PrepareAddressModel(Customer customer);
    Task DeleteAddress(Customer customer, Address address);
    Task PrepareAddressModel(CustomerAddressModel model, Address address, Customer customer, bool excludeProperties);

    Task<Address> InsertAddressModel(Customer customer, CustomerAddressModel model,
        List<CustomAttribute> customAttributes);

    Task<Address> UpdateAddressModel(Customer customer, Address address, CustomerAddressModel model,
        List<CustomAttribute> customAttributes);
}
