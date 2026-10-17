using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Messages;

public class QueuedEmailValidator : BaseGrandValidator<QueuedEmailModel>
{
    public QueuedEmailValidator(
        IEnumerable<IValidatorConsumer<QueuedEmailModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.From).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.From.Required"));
        RuleFor(x => x.To).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.To.Required"));

        RuleFor(x => x.SentTries).NotNull()
            .WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.SentTries.Required"))
            .InclusiveBetween(0, 99999)
            .WithMessage(translationService.GetResource("Admin.System.QueuedEmails.Fields.SentTries.Range"));
    }
}