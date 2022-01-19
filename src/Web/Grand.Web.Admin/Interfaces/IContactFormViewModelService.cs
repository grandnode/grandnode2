using Grand.Domain.Messages;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Interfaces
{
    public interface IContactFormViewModelService
    {
        Task<ContactFormListModel> PrepareContactFormListModel();
        Task<ContactFormModel> PrepareContactFormModel(ContactUs contactUs);
        Task<(IEnumerable<ContactFormModel> contactFormModel, int totalCount)> PrepareContactFormListModel(ContactFormListModel model, int pageIndex, int pageSize);
    }
}
