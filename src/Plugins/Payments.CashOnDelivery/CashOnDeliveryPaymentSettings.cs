using Grand.Domain.Configuration;

namespace Payments.CashOnDelivery
{
    public class CashOnDeliveryPaymentSettings : ISettings
    {
        public int DisplayOrder { get; set; }

        public string DescriptionText { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public double AdditionalFee { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether shippable products are required in order to display this payment method during checkout
        /// </summary>
        public bool ShippableProductRequired { get; set; }

        public bool SkipPaymentInfo { get; set; }
    }
}
