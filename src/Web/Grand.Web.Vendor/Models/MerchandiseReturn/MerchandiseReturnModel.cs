using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Vendor.Models.Common;

namespace Grand.Web.Vendor.Models.MerchandiseReturn;

public class MerchandiseReturnModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.ID")]
    public override string Id { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.ID")]
    public int ReturnNumber { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Order")]
    public string OrderId { get; set; }

    public int OrderNumber { get; set; }
    public string OrderCode { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.ExternalId")]
    public string ExternalId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Customer")]
    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Customer")]
    public string CustomerInfo { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Total")]
    public string Total { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.CustomerComments")]
    public string CustomerComments { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.StaffNotes")]
    public string StaffNotes { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Status")]
    public int MerchandiseReturnStatusId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Status")]
    public string MerchandiseReturnStatusStr { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.Quantity")]
    public int Quantity { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.PickupDate")]
    public DateTime PickupDate { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.Fields.PickupAddress")]
    public AddressModel PickupAddress { get; set; }

    public List<MerchandiseReturnItemModel> Items { get; set; } = new();

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.NotifyCustomer")]
    public bool NotifyCustomer { get; set; }

    //merchandise return notes
    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.DisplayToCustomer")]
    public bool AddMerchandiseReturnNoteDisplayToCustomer { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Note")]
    public string AddMerchandiseReturnNoteMessage { get; set; }

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

        [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.DisplayToCustomer")]
        public bool DisplayToCustomer { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Note")]
        public string Note { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.CreatedByCustomer")]
        public bool CreatedByCustomer { get; set; }
    }
}