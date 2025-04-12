using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class CategoryValidator : BaseGrandValidator<CategoryModel>
{
    public CategoryValidator(
        IEnumerable<IValidatorConsumer<CategoryModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Catalog.Categories.Fields.Name.Required"));
    }
}