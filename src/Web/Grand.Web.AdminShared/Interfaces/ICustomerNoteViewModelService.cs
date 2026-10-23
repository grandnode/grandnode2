using Grand.Domain.Customers;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerNoteViewModelService
{
    Task<IList<CustomerModel.CustomerNote>> PrepareCustomerNoteList(string customerId);

    Task<CustomerNote> InsertCustomerNote(string customerId, string downloadId, bool displayToCustomer, string title,
        string message);

    Task DeleteCustomerNote(string id, string customerId);
}
