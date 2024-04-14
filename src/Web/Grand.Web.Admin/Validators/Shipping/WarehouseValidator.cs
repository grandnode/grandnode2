using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Validators.Shipping;

public class WarehouseValidator : BaseGrandValidator<WarehouseModel>
{
    public WarehouseValidator(
        IEnumerable<IValidatorConsumer<WarehouseModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Fields.Name.Required"));
        RuleFor(x => x.Code).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Fields.Code.Required"));
    }
}