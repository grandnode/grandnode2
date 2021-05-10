using Grand.Infrastructure.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Interfaces
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
