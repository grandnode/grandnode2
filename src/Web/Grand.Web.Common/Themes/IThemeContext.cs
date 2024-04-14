namespace Grand.Web.Common.Themes;

/// <summary>
///     Work context
/// </summary>
public interface IThemeContext
{
    /// <summary>
    ///     Get area name
    /// </summary>
    string AreaName { get; }

    /// <summary>
    ///     Get current theme system name
    /// </summary>
    string GetCurrentTheme();

    /// <summary>
    ///     Set current theme system name
    /// </summary>
    Task SetTheme(string themeName);
}