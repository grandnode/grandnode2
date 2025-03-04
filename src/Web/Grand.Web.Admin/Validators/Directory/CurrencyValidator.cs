﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Directory;
using System.Globalization;

namespace Grand.Web.Admin.Validators.Directory;

public class CurrencyValidator : BaseGrandValidator<CurrencyModel>
{
    public CurrencyValidator(
        IEnumerable<IValidatorConsumer<CurrencyModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(
                translationService.GetResource("Admin.Configuration.Currencies.Fields.Name.Required"))
            .Length(1, 50)
            .WithMessage(translationService.GetResource("Admin.Configuration.Currencies.Fields.Name.Range"));
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage(
                translationService.GetResource("Admin.Configuration.Currencies.Fields.CurrencyCode.Required"))
            .Length(1, 5)
            .WithMessage(translationService.GetResource("Admin.Configuration.Currencies.Fields.CurrencyCode.Range"));
        RuleFor(x => x.Rate)
            .GreaterThan(0)
            .WithMessage(translationService.GetResource("Admin.Configuration.Currencies.Fields.Rate.Range"));
        RuleFor(x => x.CustomFormatting)
            .Length(0, 50)
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Currencies.Fields.CustomFormatting.Validation"));
        RuleFor(x => x.DisplayLocale)
            .Must(x =>
            {
                try
                {
                    if (string.IsNullOrEmpty(x))
                        return true;

                    var culture = new CultureInfo(x);
                    return culture != null;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Currencies.Fields.DisplayLocale.Validation"));
    }
}