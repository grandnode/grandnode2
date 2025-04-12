using Grand.Domain.Messages;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Interfaces;

public interface IContactFormViewModelService
{
    Task<ContactFormListModel> PrepareContactFormListModel();
    Task<ContactFormModel> PrepareContactFormModel(ContactUs contactUs);

    Task<(IEnumerable<ContactFormModel> contactFormModel, int totalCount)> PrepareContactFormListModel(
        ContactFormListModel model, int pageIndex, int pageSize);
}