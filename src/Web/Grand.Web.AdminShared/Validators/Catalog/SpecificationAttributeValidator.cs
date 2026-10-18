using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class SpecificationAttributeValidator : BaseGrandValidator<SpecificationAttributeModel>
{
    public SpecificationAttributeValidator(
        IEnumerable<IValidatorConsumer<SpecificationAttributeModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Attributes.SpecificationAttributes.Fields.Name.Required"));
    }
}