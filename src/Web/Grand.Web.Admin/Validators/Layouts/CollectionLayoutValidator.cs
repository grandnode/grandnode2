using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Layouts;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Layouts
{
    public class CollectionLayoutValidator : BaseGrandValidator<CollectionLayoutModel>
    {
        public CollectionLayoutValidator(
            IEnumerable<IValidatorConsumer<CollectionLayoutModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Collection.Name.Required"));
            RuleFor(x => x.ViewPath).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Collection.ViewPath.Required"));
        }
    }
}