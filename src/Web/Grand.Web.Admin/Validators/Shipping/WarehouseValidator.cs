using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Shipping;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Shipping
{
    public class WarehouseValidator : BaseGrandValidator<WarehouseModel>
    {
        public WarehouseValidator(
            IEnumerable<IValidatorConsumer<WarehouseModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Fields.Name.Required"));
        }
    }
}