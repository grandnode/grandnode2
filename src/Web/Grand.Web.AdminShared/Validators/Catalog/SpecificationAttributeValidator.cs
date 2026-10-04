using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class SpecificationAttributeValidator : BaseGrandValidator<SpecificationAttributeModel>
{
    public SpecificationAttributeValidator(
        IEnumerable<IValidatorConsumer<SpecificationAttributeModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Attributes.SpecificationAttributes.Fields.Name.Required"));
    }
}