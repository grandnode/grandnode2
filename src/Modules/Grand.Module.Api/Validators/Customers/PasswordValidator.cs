using FluentValidation;
using Grand.Module.Api.Extensions;
using Grand.Module.Api.DTOs.Customers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Customers;

public class PasswordValidator : BaseGrandValidator<PasswordDto>
{
    public PasswordValidator(
        IEnumerable<IValidatorConsumer<PasswordDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings)
        : base(validators)
    {
        var languageId = contextAccessor.WorkingLanguageId();

        RuleFor(x => x.Password).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.Password.Required", languageId));

        if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
            RuleFor(x => x.Password).Matches(customerSettings.PasswordRegularExpression)
                .WithMessage(string.Format(translationService.GetResource("Account.Fields.Password.Validation", languageId)));
    }
}