using Grand.Domain.Orders;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Orders;

public class MerchandiseReturnDetailsModel : BaseModel
{
    public string MerchandiseReturnId { get; set; }

    public IList<MerchandiseReturnItemModel> MerchandiseReturnItems { get; set; } =
        new List<MerchandiseReturnItemModel>();

    public string Comments { get; set; }

    public int ReturnNumber { get; set; }

    public string ExternalId { get; set; }

    public MerchandiseReturnStatus MerchandiseReturnStatus { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public bool ShowPickupDate { get; set; }

    public bool ShowPickupAddress { get; set; }

    public AddressModel PickupAddress { get; set; } = new();

    public DateTime PickupDate { get; set; }

    public IList<MerchandiseReturnNote> MerchandiseReturnNotes { get; set; } = new List<MerchandiseReturnNote>();

    public bool ShowAddMerchandiseReturnNote { get; set; }

    /// <summary>
    ///     How the items are sent back; null when the return has no return method
    /// </summary>
    public ReturnShippingModel ReturnShipping { get; set; }

    #region Nested Classes

    public class MerchandiseReturnNote : BaseEntityModel
    {
        public bool HasDownload { get; set; }
        public string Note { get; set; }
        public DateTime CreatedOn { get; set; }
        public string MerchandiseReturnId { get; set; }
    }

    public class MerchandiseReturnItemModel : BaseModel
    {
        public string OrderItemId { get; set; }

        public string ReasonForReturn { get; set; }

        public int Quantity { get; set; }

        public string RequestedAction { get; set; }

        public string ProductSeName { get; set; }

        public string ProductName { get; set; }

        public string ProductPrice { get; set; }
    }

    public class ReturnShippingModel
    {
        public string MethodName { get; set; }

        /// <summary>
        ///     What the customer pays, formatted in the order's currency; "Free" when nothing
        /// </summary>
        public string Cost { get; set; }

        public string DropOffPointName { get; set; }
        public string DropOffPointAddress { get; set; }
        public string DropOffPointOpeningHours { get; set; }
        public string Instructions { get; set; }
        public string ReturnCode { get; set; }
        public bool HasLabel { get; set; }

        /// <summary>
        ///     The label is not created yet, or its creation failed
        /// </summary>
        public bool LabelPending { get; set; }

        public bool CanRetryLabel { get; set; }
        public string TrackingNumber { get; set; }
        public bool CanEnterTrackingNumber { get; set; }

        public IList<TrackingEventModel> TrackingEvents { get; set; } = new List<TrackingEventModel>();
    }

    public class TrackingEventModel
    {
        public string EventName { get; set; }
        public string Location { get; set; }
        public DateTime? Date { get; set; }
    }

    #endregion
}