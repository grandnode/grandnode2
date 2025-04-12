using Grand.Domain.Messages;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Interfaces;

public interface IEmailAccountViewModelService
{
    EmailAccountModel PrepareEmailAccountModel();
    Task<EmailAccount> InsertEmailAccountModel(EmailAccountModel model);
    Task<EmailAccount> UpdateEmailAccountModel(EmailAccount emailAccount, EmailAccountModel model);
    Task SendTestEmail(EmailAccount emailAccount, EmailAccountModel model);
}