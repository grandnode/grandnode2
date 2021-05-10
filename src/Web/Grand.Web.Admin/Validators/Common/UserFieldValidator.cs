using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Common;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Common
{
    public class UserFieldValidator : BaseGrandValidator<UserFieldModel>
    {
        public UserFieldValidator(
            IEnumerable<IValidatorConsumer<UserFieldModel>> validators,
            ITranslationService translationService)
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
}