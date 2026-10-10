using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>
///     Rejects imported values that carry markup outside the allowlist. Detection only: the row is rejected and the
///     field named, the value is never rewritten. Does nothing when <see cref="SecurityConfig.EnableHtmlSanitization" /> is off.
/// </summary>
public class ImportHtmlGuard
{
    private readonly IHtmlSanitizationService _htmlSanitizationService;
    private readonly SecurityConfig _securityConfig;

    public ImportHtmlGuard(IHtmlSanitizationService htmlSanitizationService, SecurityConfig securityConfig)
    {
        _htmlSanitizationService = htmlSanitizationService;
        _securityConfig = securityConfig;
    }

    /// <summary>Fields that may hold rich text (descriptions)</summary>
    public IEnumerable<string> RichTextErrors(params (string Field, string Value)[] fields)
    {
        return Errors(fields, _htmlSanitizationService.ContainsDisallowedRichText);
    }

    /// <summary>Fields that must be plain text (names, SEO fields)</summary>
    public IEnumerable<string> PlainTextErrors(params (string Field, string Value)[] fields)
    {
        return Errors(fields, _htmlSanitizationService.ContainsMarkup);
    }

    private IEnumerable<string> Errors((string Field, string Value)[] fields, Func<string, bool> isDisallowed)
    {
        if (!_securityConfig.EnableHtmlSanitization)
            yield break;

        foreach (var (field, value) in fields)
            if (!string.IsNullOrEmpty(value) && isDisallowed(value))
                yield return $"{field} contains HTML that is not allowed.";
    }
}
