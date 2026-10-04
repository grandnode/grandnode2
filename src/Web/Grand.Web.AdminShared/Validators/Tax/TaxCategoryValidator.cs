using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Tax;

namespace Grand.Web.AdminShared.Validators.Tax;

public class TaxCategoryValidator : BaseGrandValidator<TaxCategoryModel>
{
    public TaxCategoryValidator(
        IEnumerable<IValidatorConsumer<TaxCategoryModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Tax.Categories.Fields.Name.Required"));
    }
}