namespace Grand.Infrastructure.Validators;

/// <summary>
///     Marks a bound string property that is never rich text - a meta tag, a name, an internal comment. Every tag
///     is removed and the remaining text is kept. Applied by <see cref="HtmlSanitizationFilter" />.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NoHtmlAttribute : Attribute;
