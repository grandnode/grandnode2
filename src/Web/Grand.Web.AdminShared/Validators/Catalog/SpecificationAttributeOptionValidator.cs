using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class SpecificationAttributeOptionValidator : BaseGrandValidator<SpecificationAttributeOptionModel>
{
    public SpecificationAttributeOptionValidator(
        IEnumerable<IValidatorConsumer<SpecificationAttributeOptionModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.Name.Required"));
    }
}