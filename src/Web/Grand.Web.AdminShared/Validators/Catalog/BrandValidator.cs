using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class BrandValidator : BaseGrandValidator<BrandModel>
{
    public BrandValidator(
        IEnumerable<IValidatorConsumer<BrandModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Catalog.Brands.Fields.Name.Required"));
    }
}