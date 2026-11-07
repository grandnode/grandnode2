using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Orders;

public class ShipmentModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Orders.Shipments.ID")]
    public override string Id { get; set; }

    public int ShipmentNumber { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.OrderID")]
    public string OrderId { get; set; }

    public int OrderNumber { get; set; }
    public string OrderCode { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.TotalWeight")]
    public string TotalWeight { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.TrackingNumber")]
    public string TrackingNumber { get; set; }

    public string TrackingNumberUrl { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.ShippedDate")]
    //nullable: without the hint the DateTime template renders an unset date as 01/01/0001
    [UIHint("DateTimeNullable")]
    public DateTime? ShippedDate { get; set; }

    public bool CanShip { get; set; }
    public DateTime? ShippedDateUtc { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.DeliveryDate")]
    //nullable: without the hint the DateTime template renders an unset date as 01/01/0001
    [UIHint("DateTimeNullable")]
    public DateTime? DeliveryDate { get; set; }

    public bool CanDeliver { get; set; }
    public DateTime? DeliveryDateUtc { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.AdminComment")]
    public string AdminComment { get; set; }

    public List<ShipmentItemModel> Items { get; set; } = new();

    public IList<ShipmentStatusEventModel> ShipmentStatusEvents { get; set; } = new List<ShipmentStatusEventModel>();

    //shipment notes
    [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.DisplayToCustomer")]
    public bool AddShipmentNoteDisplayToCustomer { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.Note")]
    [NoHtml]
    public string AddShipmentNoteMessage { get; set; }

    public bool AddShipmentNoteHasDownload { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.Download")]
    [UIHint("Download")]
    public string AddShipmentNoteDownloadId { get; set; }


    #region Nested classes

    public class ShipmentItemModel : BaseEntityModel
    {
        public string OrderItemId { get; set; }
        public string ProductId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.Products.ProductName")]
        public string ProductName { get; set; }

        public string Sku { get; set; }
        public string AttributeInfo { get; set; }
        public string RentalInfo { get; set; }
        public bool ShipSeparately { get; set; }

        //weight of one item (product)
        [GrandResourceDisplayName("Admin.Orders.Shipments.Products.ItemWeight")]
        public string ItemWeight { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.Products.ItemDimensions")]
        public string ItemDimensions { get; set; }

        public int QuantityToAdd { get; set; }
        public int QuantityOrdered { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.Products.QtyShipped")]
        public int QuantityInThisShipment { get; set; }

        public int QuantityInAllShipments { get; set; }

        public string ShippedFromWarehouse { get; set; }

        public bool AllowToChooseWarehouse { get; set; }

        //used before a shipment is created
        public List<WarehouseInfo> AvailableWarehouses { get; set; } = new();
        public string WarehouseId { get; set; }

        /// <summary>
        ///     The units the product tracks; None for a product tracking none
        /// </summary>
        public InventoryTrackingMode TrackingMode { get; set; }

        /// <summary>
        ///     Before a shipment is created: what each warehouse's batches give, the batch expiring first first
        /// </summary>
        public List<BatchProposalModel> BatchProposals { get; set; } = new();

        //what a shipment shipped
        public IList<string> SerialNumbers { get; set; } = new List<string>();
        public IList<ShipmentItemBatch> Batches { get; set; } = new List<ShipmentItemBatch>();
        public DateTime? WarrantyUntil { get; set; }
        public bool UnitsNotBooked { get; set; }

        #region Nested Classes

        public class BatchProposalModel
        {
            public string WarehouseId { get; set; }

            /// <summary>
            ///     The batch; empty for the stock without one
            /// </summary>
            public string Number { get; set; }

            public DateTime? ExpiryDate { get; set; }
            public BatchExpiryStatus? ExpiryStatus { get; set; }
            public int Available { get; set; }
            public int Quantity { get; set; }
        }

        public class WarehouseInfo : BaseModel
        {
            public string WarehouseId { get; set; }
            public string WarehouseCode { get; set; }
            public string WarehouseName { get; set; }
            public int StockQuantity { get; set; }
            public int ReservedQuantity { get; set; }
        }

        #endregion
    }

    public class ShipmentNote : BaseEntityModel
    {
        public string ShipmentId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.DisplayToCustomer")]
        public bool DisplayToCustomer { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.Note")]
        public string Note { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.Download")]
        public string DownloadId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.Download")]
        public Guid DownloadGuid { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Shipments.ShipmentNotes.Fields.CreatedByCustomer")]
        public bool CreatedByCustomer { get; set; }
    }

    public class ShipmentStatusEventModel : BaseModel
    {
        public string EventName { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public DateTime? Date { get; set; }
    }

    #endregion
}