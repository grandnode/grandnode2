using Grand.Domain.Configuration;
using System.Collections.Generic;

namespace Grand.Domain.Payments
{
    public class PaymentSettings : ISettings
    {
        public PaymentSettings()
        {
            ActivePaymentProviderSystemNames = new List<string>();
        }

        /// <summary>
        /// Gets or sets a system names of active payment methods
        /// </summary>
        public List<string> ActivePaymentProviderSystemNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether customers are allowed to repost (complete) payments for redirection payment methods
        /// </summary>
        public bool AllowRePostingPayments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should skip select payment provider page if we have only one payment method
        /// </summary>
        public bool SkipPaymentIfOnlyOne { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should show payment prvider if cart is zero
        /// </summary>
        public bool ShowPaymentIfCartIsZero { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show payment method descriptions on checkout pages in the public store
        /// </summary>
        public bool ShowPaymentDescriptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we skip payment info page for redirection payment provider
        /// </summary>
        public bool SkipPaymentInfo { get; set; }

    }
}