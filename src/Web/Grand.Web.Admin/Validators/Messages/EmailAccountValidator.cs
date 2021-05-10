using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class EmailAccountValidator : BaseGrandValidator<EmailAccountModel>
    {
        public EmailAccountValidator(
            IEnumerable<IValidatorConsumer<EmailAccountModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.EmailAccounts.Fields.Email"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Admin.Common.WrongEmail"));
            RuleFor(x => x.DisplayName).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.EmailAccounts.Fields.DisplayName"));
        }
    }
}