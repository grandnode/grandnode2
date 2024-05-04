using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer;

public class DeleteAccountValidator : BaseGrandValidator<DeleteAccountModel>
{
    public DeleteAccountValidator(
        IEnumerable<IValidatorConsumer<DeleteAccountModel>> validators,
        IEncryptionService encryptionService, CustomerSettings customerSettings, IWorkContext workContext,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Password).NotEmpty()
            .WithMessage(translationService.GetResource("Account.DeleteAccount.Fields.Password.Required"));
        RuleFor(x => x).Custom((x, context) =>
        {
            var pwd = workContext.CurrentCustomer.PasswordFormatId switch {
                PasswordFormat.Clear => x.Password,
                PasswordFormat.Encrypted => encryptionService.EncryptText(x.Password,
                    workContext.CurrentCustomer.PasswordSalt),
                PasswordFormat.Hashed => encryptionService.CreatePasswordHash(x.Password,
                    workContext.CurrentCustomer.PasswordSalt,
                    customerSettings.HashedPasswordFormat),
                _ => throw new Exception("PasswordFormat not supported")
            };
            var isValid = pwd == workContext.CurrentCustomer.Password;
            if (!isValid) context.AddFailure(translationService.GetResource("Account.Login.WrongCredentials"));
        });
    }
}