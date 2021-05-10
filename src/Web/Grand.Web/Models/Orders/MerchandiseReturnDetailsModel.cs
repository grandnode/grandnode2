using Grand.Domain.Orders;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;
using System;
using System.Collections.Generic;

namespace Grand.Web.Models.Orders
{
    public class MerchandiseReturnDetailsModel : BaseModel
    {
        public MerchandiseReturnDetailsModel()
        {
            MerchandiseReturnItems = new List<MerchandiseReturnItemModel>();
            PickupAddress = new AddressModel();
            MerchandiseReturnNotes = new List<MerchandiseReturnNote>();
        }

        public IList<MerchandiseReturnItemModel> MerchandiseReturnItems { get; set; }

        public string Comments { get; set; }

        public int ReturnNumber { get; set; }

        public string ExternalId { get; set; }

        public MerchandiseReturnStatus MerchandiseReturnStatus { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public bool ShowPickupDate { get; set; }

        public bool ShowPickupAddress { get; set; }

        public AddressModel PickupAddress { get; set; }

        public DateTime PickupDate { get; set; }

        public IList<MerchandiseReturnNote> MerchandiseReturnNotes { get; set; }

        public bool ShowAddMerchandiseReturnNote { get; set; }


        #region Nested Classes

        public partial class MerchandiseReturnNote : BaseEntityModel
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

        #endregion
    }
}
