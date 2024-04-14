using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Common;

namespace Grand.Web.Vendor.Validators.Common;

public class AddressValidator : BaseGrandValidator<AddressModel>
{
    public AddressValidator(
        IEnumerable<IValidatorConsumer<AddressModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.FirstName.Required"))
            .When(x => x.FirstNameEnabled && x.FirstNameRequired);
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.LastName.Required"))
            .When(x => x.LastNameEnabled && x.LastNameRequired);
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.Email.Required"))
            .When(x => x.EmailEnabled && x.EmailRequired);
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage(translationService.GetResource("Vendor.Common.WrongEmail"))
            .When(x => x.EmailEnabled && x.EmailRequired);
        RuleFor(x => x.Company)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.Company.Required"))
            .When(x => x.CompanyEnabled && x.CompanyRequired);
        RuleFor(x => x.VatNumber)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.VatNumber.Required"))
            .When(x => x.VatNumberEnabled && x.VatNumberRequired);
        RuleFor(x => x.CountryId)
            .NotNull()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.Country.Required"))
            .When(x => x.CountryEnabled);
        RuleFor(x => x.CountryId)
            .NotEqual("")
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.Country.Required"))
            .When(x => x.CountryEnabled);
        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.City.Required"))
            .When(x => x.CityEnabled && x.CityRequired);
        RuleFor(x => x.Address1)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.Address1.Required"))
            .When(x => x.StreetAddressEnabled && x.StreetAddressRequired);
        RuleFor(x => x.Address2)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.Address2.Required"))
            .When(x => x.StreetAddress2Enabled && x.StreetAddress2Required);
        RuleFor(x => x.ZipPostalCode)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.ZipPostalCode.Required"))
            .When(x => x.ZipPostalCodeEnabled && x.ZipPostalCodeRequired);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.PhoneNumber.Required"))
            .When(x => x.PhoneEnabled && x.PhoneRequired);
        RuleFor(x => x.FaxNumber)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Vendor.Address.Fields.FaxNumber.Required"))
            .When(x => x.FaxEnabled && x.FaxRequired);
    }
}