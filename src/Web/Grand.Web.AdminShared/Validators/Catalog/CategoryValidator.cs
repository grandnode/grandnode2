using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class CategoryValidator : BaseGrandValidator<CategoryModel>
{
    public CategoryValidator(
        IEnumerable<IValidatorConsumer<CategoryModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Catalog.Categories.Fields.Name.Required"));
    }
}