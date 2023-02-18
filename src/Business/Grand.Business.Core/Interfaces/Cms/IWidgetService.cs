using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Cms
{
    /// <summary>
    /// Widget service interface
    /// </summary>
    public interface IWidgetService
    {
        /// <summary>
        /// Load active widgets
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <returns>Widgets</returns>
        IList<IWidgetProvider> LoadActiveWidgets(string storeId = "", Customer customer = null);

        /// <summary>
        /// Load active widgets
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <returns>Widgets</returns>
        Task<IList<IWidgetProvider>> LoadActiveWidgetsByWidgetZone(string widgetZone, string storeId = "", Customer customer = null);

        /// <summary>
        /// Load widget by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found widget</returns>
        IWidgetProvider LoadWidgetBySystemName(string systemName);

        /// <summary>
        /// Load all widgets
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <returns>Widgets</returns>
        IList<IWidgetProvider> LoadAllWidgets(string storeId = "", Customer customer = null);
    }
}
