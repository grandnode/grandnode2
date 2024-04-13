using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Vendors;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Vendors;

public class ApplyVendorValidator : BaseGrandValidator<ApplyVendorModel>
{
    public ApplyVendorValidator(
        IEnumerable<IValidatorConsumer<ApplyVendorModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IEnumerable<IValidatorConsumer<VendorAddressModel>> addressvalidators,
        ITranslationService translationService, ICountryService countryService,
        CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        VendorSettings addressSettings)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Vendors.ApplyAccount.Name.Required"));
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Vendors.ApplyAccount.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        RuleFor(x => x.Address)
            .SetValidator(new VendorAddressValidator(addressvalidators, translationService, countryService,
                addressSettings));

        if (captchaSettings.Enabled && captchaSettings.ShowOnApplyVendorPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}