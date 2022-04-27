using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Cms
{
    /// <summary>
    /// Provides an interface for creating widgets
    /// </summary>
    public partial interface IWidgetProvider : IProvider
    {
        /// <summary>
        /// Gets widget zones where this widget for rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        Task<IList<string>> GetWidgetZones();

        /// <summary>
        /// Gets a view component name for displaying in public store
        /// </summary>
        Task<string> GetPublicViewComponentName(string widgetZone);

    }
}
