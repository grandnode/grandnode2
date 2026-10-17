using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Common;

public class UserFieldValidator : BaseGrandValidator<UserFieldModel>
{
    public UserFieldValidator(
        IEnumerable<IValidatorConsumer<UserFieldModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Common.UserFields.Fields.Id.Required"));
        RuleFor(x => x.ObjectType)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Common.UserFields.Fields.ObjectType.Required"));
        RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Common.UserFields.Fields.Key.Required"));
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Common.UserFields.Fields.Value.Required"));
    }
}