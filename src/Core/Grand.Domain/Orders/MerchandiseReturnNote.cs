using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a merchandise return note
    /// </summary>
    public partial class MerchandiseReturnNote : BaseEntity
    {
        /// <summary>
        /// Gets or sets the merchandise return identifier
        /// </summary>
        public string MerchandiseReturnId { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the attached file (download) identifier
        /// </summary>
        public string DownloadId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a customer can see a note
        /// </summary>
        public bool DisplayToCustomer { get; set; }

        /// <summary>
        /// Gets or sets the date and time of merchandise return note creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets value indicating whether this merchandise return note was create by customer
        /// </summary>
        public bool CreatedByCustomer { get; set; }
    }
}
