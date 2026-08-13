namespace Grand.Infrastructure.Validators;

/// <summary>
///     Marks a bound string property as rich text that must be sanitized against an allowlist before the action
///     body sees it. Applied by <see cref="HtmlSanitizationFilter" /> on every bound model, so it also covers the
///     API surface, and it rewrites rather than rejects - an allowlist that rewrites cannot be evaded the way a
///     pattern that rejects can.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SanitizeHtmlAttribute : Attribute;
