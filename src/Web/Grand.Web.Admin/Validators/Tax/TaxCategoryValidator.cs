using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Tax;
using System.Collections.Generic;

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