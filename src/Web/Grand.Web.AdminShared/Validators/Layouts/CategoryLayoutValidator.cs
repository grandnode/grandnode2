using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Layouts;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Layouts;

public class CategoryLayoutValidator : BaseGrandValidator<CategoryLayoutModel>
{
    public CategoryLayoutValidator(
        IEnumerable<IValidatorConsumer<CategoryLayoutModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Category.Name.Required"));
        RuleFor(x => x.ViewPath).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Category.ViewPath.Required"));
    }
}