using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Layouts;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Layouts;

public class ProductLayoutValidator : BaseGrandValidator<ProductLayoutModel>
{
    public ProductLayoutValidator(
        IEnumerable<IValidatorConsumer<ProductLayoutModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Product.Name.Required"));
        RuleFor(x => x.ViewPath).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Product.ViewPath.Required"));
    }
}