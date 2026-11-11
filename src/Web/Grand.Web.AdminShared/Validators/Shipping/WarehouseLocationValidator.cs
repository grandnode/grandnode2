using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Shipping;

public class WarehouseLocationValidator : BaseGrandValidator<WarehouseLocationModel>
{
    public WarehouseLocationValidator(
        IEnumerable<IValidatorConsumer<WarehouseLocationModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.WarehouseId).NotEmpty()
            .WithMessage(translationService.GetResource(
                "Admin.Configuration.Shipping.WarehouseLocations.Fields.Warehouse.Required"));
        RuleFor(x => x.Code).NotEmpty()
            .WithMessage(translationService.GetResource(
                "Admin.Configuration.Shipping.WarehouseLocations.Fields.Code.Required"));
    }
}
