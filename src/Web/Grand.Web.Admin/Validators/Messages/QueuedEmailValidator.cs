using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Validators.Messages
{
    public class QueuedEmailValidator : BaseGrandValidator<QueuedEmailModel>
    {
        public QueuedEmailValidator(
            IEnumerable<IValidatorConsumer<QueuedEmailModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.From).NotEmpty().WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.From.Required"));
            RuleFor(x => x.To).NotEmpty().WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.To.Required"));

            RuleFor(x => x.SentTries).NotNull().WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.SentTries.Required"))
                                    .InclusiveBetween(0, 99999).WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.SentTries.Range"));

        }
    }
}