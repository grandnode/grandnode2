using Grand.Domain.Messages;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Interfaces
{
    public interface IEmailAccountViewModelService
    {
        EmailAccountModel PrepareEmailAccountModel();
        Task<EmailAccount> InsertEmailAccountModel(EmailAccountModel model);
        Task<EmailAccount> UpdateEmailAccountModel(EmailAccount emailAccount, EmailAccountModel model);
        Task SendTestEmail(EmailAccount emailAccount, EmailAccountModel model);
    }
}
