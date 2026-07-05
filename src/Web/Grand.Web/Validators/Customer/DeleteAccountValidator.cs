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
        IEncryptionService encryptionService, CustomerSettings customerSettings, IContextAccessor contextAccessor,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Password).NotEmpty()
            .WithMessage(translationService.GetResource("Account.DeleteAccount.Fields.Password.Required"));
        RuleFor(x => x).Custom((x, context) =>
        {
            var customer = contextAccessor.WorkContext.CurrentCustomer;
            var isValid = encryptionService.VerifyPassword(x.Password, customer.PasswordFormatId, customer.Password,
                customer.PasswordSalt, customerSettings.HashedPasswordFormat);
            if (!isValid) context.AddFailure(translationService.GetResource("Account.Login.WrongCredentials"));
        });
    }
}