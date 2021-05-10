using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class MerchandiseReturnModel : BaseEntityModel
    {
        public MerchandiseReturnModel()
        {
            Items = new List<MerchandiseReturnItemModel>();
        }

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

        public List<MerchandiseReturnItemModel> Items { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.NotifyCustomer")]
        public bool NotifyCustomer { get; set; }

        //merchandise return notes
        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.DisplayToCustomer")]
        public bool AddMerchandiseReturnNoteDisplayToCustomer { get; set; }
        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Note")]
        public string AddMerchandiseReturnNoteMessage { get; set; }
        public bool AddMerchandiseReturnNoteHasDownload { get; set; }
        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.MerchandiseReturnNotes.Fields.Download")]
        [UIHint("Download")]
        public string AddMerchandiseReturnNoteDownloadId { get; set; }

        public class MerchandiseReturnItemModel : BaseEntityModel
        {
            public string ProductId { get; set; }

            public string ProductName { get; set; }

            public string UnitPrice { get; set; }

            public int Quantity { get; set; }

            public string ReasonForReturn { get; set; }

            public string RequestedAction { get; set; }
        }

        public partial class MerchandiseReturnNote : BaseEntityModel
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
}