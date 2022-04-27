using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Infrastructure.TypeSearchers;

namespace Grand.Business.Checkout.Services.Shipping
{
    /// <summary>
    /// General shipment tracker (finds an appropriate tracker by tracking number)
    /// </summary>
    public partial class GeneralShipmentTracker : IShipmentTracker
    {
        #region Fields

        private readonly ITypeSearcher _typeSearcher;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="typeSearcher">Type finder</param>
        public GeneralShipmentTracker(ITypeSearcher typeSearcher)
        {
            _typeSearcher = typeSearcher;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets all trackers
        /// </summary>
        /// <returns>All available shipment trackers</returns>
        protected virtual IList<IShipmentTracker> GetAllTrackers()
        {
            return _typeSearcher.ClassesOfType<IShipmentTracker>()
                //exclude this one
                .Where(x => x != typeof(GeneralShipmentTracker))
                .Select(x => x as IShipmentTracker)
                .ToList();
        }

        protected virtual async Task<IShipmentTracker> GetTrackerByTrackingNumber(string trackingNumber)
        {
            foreach (var item in GetAllTrackers())
            {
                if (await item.IsMatch(trackingNumber))
                    return item;
            }
            return null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets if the current tracker can track the tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>True if the tracker can track, otherwise false.</returns>
        public virtual async Task<bool> IsMatch(string trackingNumber)
        {
            var tracker = await GetTrackerByTrackingNumber(trackingNumber);
            if (tracker != null)
                return await tracker.IsMatch(trackingNumber);
            return false;
        }

        /// <summary>
        /// Gets a url for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>A url to a tracking page.</returns>
        public virtual async Task<string> GetUrl(string trackingNumber)
        {
            var tracker = await GetTrackerByTrackingNumber(trackingNumber);
            if (tracker != null)
                return await tracker.GetUrl(trackingNumber);
            return null;
        }

        /// <summary>
        /// Gets all events for a tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>List of Shipment Events.</returns>
        public virtual async Task<IList<ShipmentStatusEvent>> GetShipmentEvents(string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
                return new List<ShipmentStatusEvent>();

            var tracker = await GetTrackerByTrackingNumber(trackingNumber);
            if (tracker != null)
                return await tracker.GetShipmentEvents(trackingNumber);
            return new List<ShipmentStatusEvent>();
        }

        #endregion
    }
}
