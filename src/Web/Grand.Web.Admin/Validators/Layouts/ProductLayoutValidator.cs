using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Layouts;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Layouts
{
    public class ProductLayoutValidator : BaseGrandValidator<ProductLayoutModel>
    {
        public ProductLayoutValidator(
            IEnumerable<IValidatorConsumer<ProductLayoutModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Product.Name.Required"));
            RuleFor(x => x.ViewPath).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Product.ViewPath.Required"));
        }
    }
}