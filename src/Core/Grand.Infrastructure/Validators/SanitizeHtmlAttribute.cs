using System.ComponentModel.DataAnnotations;
using Grand.Infrastructure.Security;

namespace Grand.Infrastructure.Validators;

/// <summary>
///     Rejects a bound string property when it contains markup outside the rich-text allowlist - a tag,
///     attribute, style rule, or url (script, iframe to an unlisted host, javascript:, entity-encoded scheme,
///     etc.) that can execute or navigate on its own. Detection runs through <see cref="IHtmlSanitizationService" />,
///     a real HTML parser rather than a regular expression, so it is not defeated by whitespace, casing, or
///     encoding tricks the way a pattern match is.
///     <see cref="IHtmlSanitizationService" /> is resolved from <see cref="ValidationContext.GetService" /> rather
///     than constructor injection - ASP.NET Core's model validation constructs the ValidationContext with
///     HttpContext.RequestServices, so this is the supported way for a DataAnnotations attribute to reach a
///     scoped/singleton service.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SanitizeHtmlAttribute() : ValidationAttribute("{0} contains HTML markup that is not allowed.")
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is not string html || string.IsNullOrWhiteSpace(html)) return ValidationResult.Success;

        var sanitizationService = (IHtmlSanitizationService)validationContext
            .GetService(typeof(IHtmlSanitizationService));

        if (sanitizationService is null)
            throw new InvalidOperationException(
                $"{nameof(IHtmlSanitizationService)} could not be resolved - is it registered in DI?");

        return sanitizationService.ContainsDisallowedRichText(html)
            ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName),
                MemberNames(validationContext))
            : ValidationResult.Success;
    }

    private static IEnumerable<string> MemberNames(ValidationContext validationContext)
    {
        return validationContext.MemberName is null ? [] : [validationContext.MemberName];
    }
}
