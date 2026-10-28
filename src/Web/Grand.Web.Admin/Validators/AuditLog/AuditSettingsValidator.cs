using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.AuditLog;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Validators.AuditLog;

public class AuditSettingsValidator : BaseGrandValidator<AuditSettingsModel>
{
    public AuditSettingsValidator(IEnumerable<IValidatorConsumer<AuditSettingsModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.RetentionDays).GreaterThanOrEqualTo(1)
            .WithMessage(translationService.GetResource("Admin.System.AuditLog.Fields.RetentionDays.Min"));
    }
}
