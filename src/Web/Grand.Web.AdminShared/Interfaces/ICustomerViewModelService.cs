using Grand.Domain.Customers;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerViewModelService
{
    Task<CustomerListModel> PrepareCustomerListModel();

    Task<(IEnumerable<CustomerModel> customerModelList, int totalCount)> PrepareCustomerList(CustomerListModel model,
        string[] searchCustomerGroupIds, string[] searchCustomerTagIds, int pageIndex, int pageSize,
        string storeId = "");

    Task PrepareCustomerModel(CustomerModel model, Customer customer, bool excludeProperties);
    Task<Customer> InsertCustomerModel(CustomerModel model);
    Task<Customer> UpdateCustomerModel(Customer customer, CustomerModel model);
    Task DeleteCustomer(Customer customer);
    Task DeleteSelected(IEnumerable<string> selectedIds);
    Task SendEmail(Customer customer, CustomerModel.SendEmailModel model);
}
