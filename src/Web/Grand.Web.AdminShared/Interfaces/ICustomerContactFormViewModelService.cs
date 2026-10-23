using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerContactFormViewModelService
{
    Task<(IEnumerable<ContactFormModel> contactFormModels, int totalCount)> PrepareContactFormModel(string customerId,
        int pageIndex, int pageSize);
}
