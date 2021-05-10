//Contribution https://github.com/smartstore/SmartStoreNET/blob/2.0.x/src/Presentation/SmartStore.Web.Framework/Themes/IThemeContext.cs

using System.Threading.Tasks;

namespace Grand.Web.Common.Themes
{
    /// <summary>
    /// Work context
    /// </summary>
    public interface IThemeContext
    {
        /// <summary>
        /// Get current theme system name
        /// </summary>
        string WorkingThemeName { get; }

        /// <summary>
        /// Get admin area current theme name
        /// </summary>
        string AdminAreaThemeName { get; }

        /// <summary>
        /// Set current theme system name
        /// </summary>
        Task SetWorkingTheme(string themeName);
    }
}
