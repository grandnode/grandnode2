using Grand.Domain.Configuration;

namespace Grand.Domain.Common
{
    public class SystemSettings : ISettings
    {
        /// <summary>
        /// Gets or sets interval (in minutes) with which the Delete Guest Task runs
        /// </summary>
        public int DeleteGuestTaskOlderThanMinutes { get; set; } = 1440;

        /// <summary>
        /// Gets or sets Number of Days after which order would automatically Canceled - if not paid and has pending status
        /// </summary>
        public int? DaysToCancelUnpaidOrder { get; set; }

    }
}
