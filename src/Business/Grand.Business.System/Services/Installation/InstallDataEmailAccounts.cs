using Grand.Domain.Messages;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallEmailAccounts()
    {
        var emailAccounts = new List<EmailAccount> {
            new() {
                Email = "test@mail.com",
                DisplayName = "Store name",
                Host = "smtp.mail.com",
                Port = 25,
                Username = "123",
                Password = "123",
                SecureSocketOptionsId = 1,
                UseServerCertificateValidation = true
            }
        };
        emailAccounts.ForEach(x => _emailAccountRepository.Insert(x));
        return Task.CompletedTask;
    }
}