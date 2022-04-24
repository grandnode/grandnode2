using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Layouts;

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