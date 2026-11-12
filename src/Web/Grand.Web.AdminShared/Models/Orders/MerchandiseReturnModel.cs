using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Orders;

public class MerchandiseReturnModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.ID")]
    public override string Id { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.ID")]
    public int ReturnNumber { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Order")]
    public string OrderId { get; set; }

    public int OrderNumber { get; set; }
    public string OrderCode { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.ExternalId")]
    public string ExternalId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Customer")]
    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Customer")]
    public string CustomerInfo { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Total")]
    public string Total { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.CustomerComments")]
    public string CustomerComments { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.StaffNotes")]
    public string StaffNotes { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Status")]
    public int MerchandiseReturnStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Status")]
    public string MerchandiseReturnStatusStr { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.Quantity")]
    public int Quantity { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.PickupDate")]
    public DateTime PickupDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.Fields.PickupAddress")]
    public AddressModel PickupAddress { get; set; }

    public List<MerchandiseReturnItemModel> Items { get; set; } = new();

    //the statuses the return may change to, each a button on the edit page
    public IList<int> NextStatuses { get; set; } = new List<int>();

    //posted by a status button: the form is saved first, then the status changes
    public int? ChangeStatusTo { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.ChangeStatusNote")]
    [NoHtml]
    public string ChangeStatusNote { get; set; }

    //merchandise return notes
    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.DisplayToCustomer")]
    public bool AddMerchandiseReturnNoteDisplayToCustomer { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Note")]
    [NoHtml]
    public string AddMerchandiseReturnNoteMessage { get; set; }

    public bool AddMerchandiseReturnNoteHasDownload { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Download")]
    [UIHint("Download")]
    public string AddMerchandiseReturnNoteDownloadId { get; set; }

    //how the items are sent back; null when the return has no return method, and the card is not shown
    public ReturnShippingModel ReturnShipping { get; set; }

    public class ReturnShippingModel : BaseModel
    {
        //the provider's name and the option the customer chose
        public string MethodName { get; set; }
        public string OptionName { get; set; }

        //formatted in the order's currency
        public string Cost { get; set; }

        public string DropOffPoint { get; set; }

        //the one field the staff edit: saved through the label service, never with the rest of the form
        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.ReturnShipping.TrackingNumber")]
        [NoHtml]
        public string TrackingNumber { get; set; }

        //the tracking number the page was shown with, posted back: a number written since (the label, the
        //customer) is kept unless the staff typed another one, and even then only while it is still this one
        public string ShownTrackingNumber { get; set; }

        public string ReturnCode { get; set; }
        public string Instructions { get; set; }
        public bool HasLabel { get; set; }
        public string LabelStatus { get; set; }

        //the provider's error, for the staff only
        public string LabelError { get; set; }

        public bool CanRetryLabel { get; set; }
        public IList<TrackingEventModel> TrackingEvents { get; set; } = new List<TrackingEventModel>();
    }

    public class TrackingEventModel : BaseModel
    {
        public string EventName { get; set; }
        public string Location { get; set; }
        public DateTime? Date { get; set; }
    }

    public class MerchandiseReturnItemModel : BaseEntityModel
    {
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductSku { get; set; }

        public string UnitPrice { get; set; }

        public int Quantity { get; set; }

        public string ReasonForReturn { get; set; }

        public string RequestedAction { get; set; }
    }

    public class MerchandiseReturnNote : BaseEntityModel
    {
        public string MerchandiseReturnId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.DisplayToCustomer")]
        public bool DisplayToCustomer { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Note")]
        public string Note { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Download")]
        public string DownloadId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Download")]
        public Guid DownloadGuid { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.CreatedByCustomer")]
        public bool CreatedByCustomer { get; set; }
    }
}