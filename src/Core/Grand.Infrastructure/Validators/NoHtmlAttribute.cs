using System.ComponentModel.DataAnnotations;
using Grand.Infrastructure.Security;

namespace Grand.Infrastructure.Validators;

/// <summary>
///     Rejects a bound string property that contains any HTML markup at all - for a property that is never rich
///     text: a meta tag, a name, an internal comment. See <see cref="SanitizeHtmlAttribute" /> for why detection
///     goes through <see cref="IHtmlSanitizationService" /> resolved via <see cref="ValidationContext.GetService" />.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NoHtmlAttribute() : ValidationAttribute("{0} must be plain text and cannot contain HTML markup.")
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return ValidationResult.Success;

        var sanitizationService = (IHtmlSanitizationService)validationContext
            .GetService(typeof(IHtmlSanitizationService));

        if (sanitizationService is null)
            throw new InvalidOperationException(
                $"{nameof(IHtmlSanitizationService)} could not be resolved - is it registered in DI?");

        return sanitizationService.ContainsMarkup(text)
            ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName),
                MemberNames(validationContext))
            : ValidationResult.Success;
    }

    private static IEnumerable<string> MemberNames(ValidationContext validationContext)
    {
        return validationContext.MemberName is null ? [] : [validationContext.MemberName];
    }
}
