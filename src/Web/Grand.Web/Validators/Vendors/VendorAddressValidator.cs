using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Vendors;

namespace Grand.Web.Validators.Vendors;

public class VendorAddressValidator : BaseGrandValidator<VendorAddressModel>
{
    public VendorAddressValidator(
        IEnumerable<IValidatorConsumer<VendorAddressModel>> validators,
        ITranslationService translationService,
        ICountryService countryService,
        VendorSettings addressSettings)
        : base(validators)
    {
        if (addressSettings.CountryEnabled)
        {
            RuleFor(x => x.CountryId)
                .NotNull()
                .WithMessage(translationService.GetResource("Account.VendorInfo.Country.Required"));
            RuleFor(x => x.CountryId)
                .NotEqual("")
                .WithMessage(translationService.GetResource("Account.VendorInfo.Country.Required"));
        }

        if (addressSettings.CountryEnabled && addressSettings.StateProvinceEnabled)
            RuleFor(x => x.StateProvinceId).MustAsync(async (x, y, _) =>
            {
                var countryId = !string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "";
                var country = await countryService.GetCountryById(countryId);
                if (country == null || !country.StateProvinces.Any()) return false;
                //if yes, then ensure that state is selected
                if (string.IsNullOrEmpty(y)) return false;
                return country.StateProvinces.FirstOrDefault(s => s.Id == y) != null;
            }).WithMessage(translationService.GetResource("Account.VendorInfo.StateProvince.Required"));
        if (addressSettings.CompanyRequired && addressSettings.CompanyEnabled)
            RuleFor(x => x.Company).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.Company.Required"));
        if (addressSettings.StreetAddressRequired && addressSettings.StreetAddressEnabled)
            RuleFor(x => x.Address1).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.Address1.Required"));
        if (addressSettings.StreetAddress2Required && addressSettings.StreetAddress2Enabled)
            RuleFor(x => x.Address2).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.Address2.Required"));
        if (addressSettings.ZipPostalCodeRequired && addressSettings.ZipPostalCodeEnabled)
            RuleFor(x => x.ZipPostalCode).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.ZipPostalCode.Required"));
        if (addressSettings.CityRequired && addressSettings.CityEnabled)
            RuleFor(x => x.City).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.City.Required"));
        if (addressSettings.PhoneRequired && addressSettings.PhoneEnabled)
            RuleFor(x => x.PhoneNumber).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.phonenumber.Required"));
        if (addressSettings.FaxRequired && addressSettings.FaxEnabled)
            RuleFor(x => x.FaxNumber).NotEmpty()
                .WithMessage(translationService.GetResource("Account.VendorInfo.faxnumber.Required"));
    }
}