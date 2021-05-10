using Grand.Business.Messages.Interfaces;
using Grand.Domain.Messages;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class EmailAccountViewModelService : IEmailAccountViewModelService
    {
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;

        public EmailAccountViewModelService(IEmailAccountService emailAccountService, IEmailSender emailSender)
        {
            _emailAccountService = emailAccountService;
            _emailSender = emailSender;
        }
        public virtual EmailAccountModel PrepareEmailAccountModel()
        {
            var model = new EmailAccountModel
            {
                //default values
                Port = 25
            };
            return model;
        }
        public virtual async Task<EmailAccount> InsertEmailAccountModel(EmailAccountModel model)
        {
            var emailAccount = model.ToEntity();
            //set password manually
            emailAccount.Password = model.Password;
            await _emailAccountService.InsertEmailAccount(emailAccount);
            return emailAccount;
        }
        public virtual async Task<EmailAccount> UpdateEmailAccountModel(EmailAccount emailAccount, EmailAccountModel model)
        {
            emailAccount = model.ToEntity(emailAccount);
            if (!string.IsNullOrEmpty(model.Password))
                emailAccount.Password = model.Password;

            await _emailAccountService.UpdateEmailAccount(emailAccount);
            return emailAccount;
        }
        
        public virtual async Task SendTestEmail(EmailAccount emailAccount, EmailAccountModel model)
        {
            string subject = "Testing email functionality.";
            string body = "Email works fine.";
            await _emailSender.SendEmail(emailAccount, subject, body, emailAccount.Email, emailAccount.DisplayName, model.SendTestEmailTo, null);
        }
    }
}
