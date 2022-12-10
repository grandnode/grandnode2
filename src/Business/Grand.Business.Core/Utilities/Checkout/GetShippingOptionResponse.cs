using Grand.Domain.Shipping;

namespace Grand.Business.Core.Utilities.Checkout
{
    /// <summary>
    /// Represents a response of getting shipping rate options
    /// </summary>
    public class GetShippingOptionResponse
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public GetShippingOptionResponse()
        {
            Errors = new List<string>();
            ShippingOptions = new List<ShippingOption>();
        }

        /// <summary>
        /// Gets or sets a list of shipping options
        /// </summary>
        public IList<ShippingOption> ShippingOptions { get; set; }

        /// <summary>
        /// Gets or sets an address
        /// </summary>
        public IList<string> Errors { get; set; }

        /// <summary>
        /// Gets a value indicating whether request has been completed successfully
        /// </summary>
        public bool Success => Errors.Count == 0;

        /// <summary>
        /// Add error
        /// </summary>
        /// <param name="error">Error</param>
        public void AddError(string error)
        {
            Errors.Add(error);
        }
    }
}
