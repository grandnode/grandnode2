using FluentValidation;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Catalog;

public class SpecificationAttributeValidator : BaseGrandValidator<SpecificationAttributeDto>
{
    public SpecificationAttributeValidator(
        IEnumerable<IValidatorConsumer<SpecificationAttributeDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor, ISpecificationAttributeService specificationAttributeService)
        : base(validators)
    {
        var languageId = contextAccessor.WorkContext.WorkingLanguage.Id;

        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Api.Catalog.SpecificationAttribute.Fields.Name.Required", languageId));
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var sa = await specificationAttributeService.GetSpecificationAttributeById(x.Id);
                if (sa == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.SpecificationAttribute.Fields.Id.NotExists", languageId));
        RuleFor(x => x).Must((x, _) =>
        {
            return x.SpecificationAttributeOptions.All(item => !string.IsNullOrEmpty(item.Name));
        }).WithMessage(
            translationService.GetResource("Api.Catalog.SpecificationAttributeOptions.Fields.Name.Required", languageId));
    }
}