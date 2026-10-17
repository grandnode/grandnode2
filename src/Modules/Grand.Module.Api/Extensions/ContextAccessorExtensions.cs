using Grand.Infrastructure;

namespace Grand.Module.Api.Extensions;

public static class ContextAccessorExtensions
{
    /// <summary>
    ///     Gets the working language id of the current request, or an empty string when none is resolved
    /// </summary>
    public static string WorkingLanguageId(this IContextAccessor contextAccessor)
    {
        return contextAccessor.WorkContext?.WorkingLanguage?.Id ?? string.Empty;
    }
}
