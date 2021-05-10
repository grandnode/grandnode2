using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class ProductAttributeValidator : BaseGrandValidator<ProductAttributeModel>
    {
        public ProductAttributeValidator(
            IEnumerable<IValidatorConsumer<ProductAttributeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Fields.Name.Required"));
        }
    }
}