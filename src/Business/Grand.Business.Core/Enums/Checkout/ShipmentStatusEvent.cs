namespace Grand.Business.Core.Enums.Checkout
{
    /// <summary>
    /// Shipment status event
    /// </summary>
    public class ShipmentStatusEvent
    {
        /// <summary>
        /// Event name
        /// </summary>
        public string EventName { get; set; }
        /// <summary>
        /// Location
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// two-letter country code
        /// </summary>
        public string CountryCode { get; set; }
        /// <summary>
        /// Date
        /// </summary>
        public DateTime? Date { get; set; }
    }
}
