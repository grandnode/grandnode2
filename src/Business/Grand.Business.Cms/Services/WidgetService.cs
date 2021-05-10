using Grand.Business.Cms.Interfaces;
using Grand.Domain.Cms;
using Grand.Domain.Customers;
using Grand.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Services
{
    /// <summary>
    /// Widget service
    /// </summary>
    public partial class WidgetService : IWidgetService
    {
        #region Fields

        private readonly IEnumerable<IWidgetProvider> _widgetProviders;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public WidgetService(
            IEnumerable<IWidgetProvider> widgetProviders,
            WidgetSettings widgetSettings)
        {
            _widgetProviders = widgetProviders;
            _widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load active widgets
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <returns>Widgets</returns>
        public virtual IList<IWidgetProvider> LoadActiveWidgets(string storeId = "", Customer customer = null)
        {
            return LoadAllWidgets(storeId, customer)
                   .Where(x => 
                   _widgetSettings.ActiveWidgetSystemNames.Contains(x.SystemName, StringComparer.OrdinalIgnoreCase))
                   .ToList();
        }

        /// <summary>
        /// Load active widgets
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <returns>Widgets</returns>
        public virtual async Task<IList<IWidgetProvider>> LoadActiveWidgetsByWidgetZone(string widgetZone, string storeId = "", Customer customer = null)
        {
            if (string.IsNullOrWhiteSpace(widgetZone))
                return new List<IWidgetProvider>();

            var activeWidgets = new List<IWidgetProvider>();
            var widgets = LoadActiveWidgets(storeId, customer).ToList();
            foreach (var widget in widgets)
            {
                var widgetZones = await widget.GetWidgetZones();
                if (widgetZones.Contains(widgetZone, StringComparer.OrdinalIgnoreCase))
                    activeWidgets.Add(widget);
            }
            return activeWidgets;
        }

        /// <summary>
        /// Load widget by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found widget</returns>
        public virtual IWidgetProvider LoadWidgetBySystemName(string systemName)
        {
            return _widgetProviders.FirstOrDefault(x => x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Load all widgets
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <returns>Widgets</returns>
        public virtual IList<IWidgetProvider> LoadAllWidgets(string storeId = "", Customer customer = null)
        {
            return _widgetProviders
                .Where(x => 
                    x.IsAuthenticateStore(storeId) && 
                    x.IsAuthenticateGroup(customer))
                .OrderBy(x => x.Priority).ToList();
        }

        #endregion
    }
}
