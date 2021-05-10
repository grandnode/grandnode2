using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Stores;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Stores
{
    public class StoreValidator : BaseGrandValidator<StoreModel>
    {
        public StoreValidator(
            IEnumerable<IValidatorConsumer<StoreModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Name.Required"));
            RuleFor(x => x.Shortcut).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Shortcut.Required"));
            RuleFor(x => x.Url).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Stores.Fields.Url.Required"));
        }
    }
}