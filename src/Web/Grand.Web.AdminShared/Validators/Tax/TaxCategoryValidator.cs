using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Tax;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Tax;

public class TaxCategoryValidator : BaseGrandValidator<TaxCategoryModel>
{
    public TaxCategoryValidator(
        IEnumerable<IValidatorConsumer<TaxCategoryModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Tax.Categories.Fields.Name.Required"));
    }
}