using FluentValidation;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Contact;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Contact;

public class ContactUsValidator : BaseGrandValidator<ContactUsModel>
{
    private readonly IContactAttributeParser _contactAttributeParser;
    private readonly IContactAttributeService _contactAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public ContactUsValidator(
        IEnumerable<IValidatorConsumer<ContactUsModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IContactAttributeParser contactAttributeParser,
        IContactAttributeService contactAttributeService,
        IWorkContext workContext,
        ITranslationService translationService, CommonSettings commonSettings,
        CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator)
        : base(validators)
    {
        _contactAttributeParser = contactAttributeParser;
        _contactAttributeService = contactAttributeService;
        _translationService = translationService;
        _workContext = workContext;

        RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("ContactUs.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        RuleFor(x => x.FullName).NotEmpty()
            .WithMessage(translationService.GetResource("ContactUs.FullName.Required"));
        if (commonSettings.SubjectFieldOnContactUsForm)
            RuleFor(x => x.Subject).NotEmpty()
                .WithMessage(translationService.GetResource("ContactUs.Subject.Required"));

        RuleFor(x => x.Enquiry).NotEmpty()
            .WithMessage(translationService.GetResource("ContactUs.Enquiry.Required"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnContactUsPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var contactAttributeWarnings = await GetContactAttributesWarnings(
                x.Attributes.Select(z => new CustomAttribute { Key = z.Key, Value = z.Value }).ToList(),
                workContext.CurrentStore.Id);
            if (contactAttributeWarnings.Any())
                foreach (var item in contactAttributeWarnings)
                    context.AddFailure(item);
        });
    }

    private async Task<IList<string>> GetContactAttributesWarnings(IList<CustomAttribute> customAttributes,
        string storeId)
    {
        var warnings = new List<string>();

        //selected attributes
        var attributes1 = await _contactAttributeParser.ParseContactAttributes(customAttributes);

        //existing contact attributes
        var attributes2 = await _contactAttributeService.GetAllContactAttributes(storeId);
        foreach (var a2 in attributes2)
        {
            var conditionMet = await _contactAttributeParser.IsConditionMet(a2, customAttributes);
            if (!a2.IsRequired ||
                ((!conditionMet.HasValue || !conditionMet.Value) && conditionMet.HasValue)) continue;
            var found = false;
            //selected checkout attributes
            foreach (var a1 in attributes1)
            {
                if (a1.Id != a2.Id) continue;
                var attributeValuesStr = customAttributes.Where(x => x.Key == a1.Id).Select(x => x.Value).ToList();
                foreach (var str1 in attributeValuesStr)
                    if (!string.IsNullOrEmpty(str1.Trim()))
                    {
                        found = true;
                        break;
                    }
            }

            //if not found
            if (!found)
                warnings.Add(
                    !string.IsNullOrEmpty(a2.GetTranslation(a => a.TextPrompt, _workContext.WorkingLanguage.Id))
                        ? a2.GetTranslation(a => a.TextPrompt, _workContext.WorkingLanguage.Id)
                        : string.Format(_translationService.GetResource("ContactUs.SelectAttribute"),
                            a2.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id)));
        }

        //now validation rules

        //minimum length
        foreach (var ca in attributes2)
        {
            if (ca.ValidationMinLength.HasValue)
                if (ca.AttributeControlType is AttributeControlType.TextBox
                    or AttributeControlType.MultilineTextbox)
                {
                    var conditionMet = await _contactAttributeParser.IsConditionMet(ca, customAttributes);
                    if (ca.IsRequired && ((conditionMet.HasValue && conditionMet.Value) || !conditionMet.HasValue))
                    {
                        var valuesStr = customAttributes.Where(x => x.Key == ca.Id).Select(x => x.Value).ToList();
                        var enteredText = valuesStr.FirstOrDefault();
                        var enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                        if (ca.ValidationMinLength.Value > enteredTextLength)
                            warnings.Add(string.Format(
                                _translationService.GetResource("ContactUs.TextboxMinimumLength"),
                                ca.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id),
                                ca.ValidationMinLength.Value));
                    }
                }

            //maximum length
            if (!ca.ValidationMaxLength.HasValue) continue;
            {
                if (ca.AttributeControlType != AttributeControlType.TextBox &&
                    ca.AttributeControlType != AttributeControlType.MultilineTextbox) continue;
                var conditionMet = await _contactAttributeParser.IsConditionMet(ca, customAttributes);
                if (!ca.IsRequired || ((!conditionMet.HasValue || !conditionMet.Value) && conditionMet.HasValue))
                    continue;
                var valuesStr = customAttributes.Where(x => x.Key == ca.Id).Select(x => x.Value).ToList();
                var enteredText = valuesStr.FirstOrDefault();
                var enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                if (ca.ValidationMaxLength.Value < enteredTextLength)
                    warnings.Add(string.Format(_translationService.GetResource("ContactUs.TextboxMaximumLength"),
                        ca.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id),
                        ca.ValidationMaxLength.Value));
            }
        }

        return warnings;
    }
}