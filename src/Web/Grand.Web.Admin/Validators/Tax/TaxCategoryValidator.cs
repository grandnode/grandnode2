using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Tax;

namespace Grand.Web.Admin.Validators.Tax
{
    public class TaxCategoryValidator : BaseGrandValidator<TaxCategoryModel>
    {
        public TaxCategoryValidator(
            IEnumerable<IValidatorConsumer<TaxCategoryModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Tax.Categories.Fields.Name.Required"));
        }
    }
}