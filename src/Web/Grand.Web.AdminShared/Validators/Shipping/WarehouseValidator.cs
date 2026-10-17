using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Shipping;

public class WarehouseValidator : BaseGrandValidator<WarehouseModel>
{
    public WarehouseValidator(
        IEnumerable<IValidatorConsumer<WarehouseModel>> validators,
        IRequestTranslationService translationService)
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