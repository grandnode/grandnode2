using Grand.Domain.Customers;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerGroupViewModelService
{
    CustomerGroupModel PrepareCustomerGroupModel(CustomerGroup customerGroup);
    CustomerGroupModel PrepareCustomerGroupModel();
    Task<CustomerGroup> InsertCustomerGroupModel(CustomerGroupModel model);
    Task<CustomerGroup> UpdateCustomerGroupModel(CustomerGroup customerGroup, CustomerGroupModel model);
    Task DeleteCustomerGroup(CustomerGroup customerGroup);

    Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        CustomerGroupProductModel.AddProductModel model, int pageIndex, int pageSize);

    Task<IList<CustomerGroupProductModel>> PrepareCustomerGroupProductModel(string customerGroupId);
    Task<CustomerGroupProductModel.AddProductModel> PrepareProductModel(string customerGroupId);
    Task InsertProductModel(CustomerGroupProductModel.AddProductModel model);
}