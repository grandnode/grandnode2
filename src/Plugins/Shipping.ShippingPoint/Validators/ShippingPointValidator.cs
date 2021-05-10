using FluentValidation;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using Shipping.ShippingPoint.Models;
using System.Collections.Generic;

namespace Shipping.ShippingPoint.Validators
{
    public class ShippingPointValidator : BaseGrandValidator<ShippingPointModel>
    {
        public ShippingPointValidator(
            IEnumerable<IValidatorConsumer<ShippingPointModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.ShippingPointName).NotEmpty().WithMessage(translationService.GetResource("Shipping.ShippingPoint.RequiredShippingPointName"));
            RuleFor(x => x.Description).NotEmpty().WithMessage(translationService.GetResource("Shipping.ShippingPoint.RequiredDescription"));
            RuleFor(x => x.OpeningHours).NotEmpty().WithMessage(translationService.GetResource("Shipping.ShippingPoint.RequiredOpeningHours"));
            RuleFor(x => x.CountryId).NotNull().WithMessage(translationService.GetResource("Admin.Address.Fields.Country.Required"));
            RuleFor(x => x.City).NotEmpty().WithMessage(translationService.GetResource("Admin.Address.Fields.City.Required"));
            RuleFor(x => x.Address1).NotEmpty().WithMessage(translationService.GetResource("Admin.Address.Fields.Address1.Required"));
            RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(translationService.GetResource("Admin.Address.Fields.ZipPostalCode.Required"));
        }
    }

}