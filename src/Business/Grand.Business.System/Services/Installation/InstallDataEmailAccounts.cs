﻿using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Messages;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
    {
        protected virtual async Task InstallEmailAccounts()
        {
            var emailAccounts = new List<EmailAccount>
                               {
                                   new EmailAccount
                                       {
                                           Email = "test@mail.com",
                                           DisplayName = "Store name",
                                           Host = "smtp.mail.com",
                                           Port = 25,
                                           Username = "123",
                                           Password = "123",
                                           SecureSocketOptionsId = 1,
                                           UseServerCertificateValidation = true
                                       },
                               };
            await _emailAccountRepository.InsertAsync(emailAccounts);
        }
    }
}
