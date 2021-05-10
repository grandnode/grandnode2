using Grand.Domain.Common;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a merchandise return
    /// </summary>
    public partial class MerchandiseReturn : BaseEntity
    {
        public MerchandiseReturn()
        {
            MerchandiseReturnItems = new List<MerchandiseReturnItem>();
        }

        public int ReturnNumber { get; set; }

        /// <summary>
        /// Gets or sets the ExternalId
        /// </summary>
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the order item identifier
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Gets or sets the owner item identifier
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the sales employee identifier 
        /// </summary>
        public string SeId { get; set; }

        /// <summary>
        /// Gets or sets the merchandise return items
        /// </summary>
        public IList<MerchandiseReturnItem> MerchandiseReturnItems { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the vendor item identifier
        /// </summary>
        public string VendorId { get; set; }

        /// <summary>
        /// Gets or sets the customer comments
        /// </summary>
        public string CustomerComments { get; set; }

        /// <summary>
        /// Gets or sets the staff notes
        /// </summary>
        public string StaffNotes { get; set; }

        /// <summary>
        /// Gets or sets the return status identifier
        /// </summary>
        public int MerchandiseReturnStatusId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the return status
        /// </summary>
        public MerchandiseReturnStatus MerchandiseReturnStatus
        {
            get
            {
                return (MerchandiseReturnStatus)MerchandiseReturnStatusId;
            }
            set
            {
                MerchandiseReturnStatusId = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the pickup date
        /// </summary>
        public DateTime PickupDate { get; set; }

        /// <summary>
        /// Gets or sets the pickup address
        /// </summary>
        public Address PickupAddress { get; set; }

        /// <summary>
        /// Get or sets notify customer
        /// </summary>
        public bool NotifyCustomer { get; set; }
    }
}
