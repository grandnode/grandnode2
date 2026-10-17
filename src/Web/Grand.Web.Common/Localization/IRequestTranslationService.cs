using Grand.Business.Core.Interfaces.Common.Localization;

namespace Grand.Web.Common.Localization;

/// <summary>
///     Translation service for code that runs inside a request
/// </summary>
public interface IRequestTranslationService : ITranslationService
{
    /// <summary>
    ///     Gets a resource string in the working language of the current request.
    /// </summary>
    /// <param name="name">A string representing a name.</param>
    /// <returns>A string representing the requested resource string.</returns>
    string GetResource(string name);
}
