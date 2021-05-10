using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;
using System;
using System.Collections.Generic;

namespace Grand.Web.Models.Orders
{
    public partial class MerchandiseReturnModel : BaseModel
    {
        public MerchandiseReturnModel()
        {
            Items = new List<OrderItemModel>();
            AvailableReturnReasons = new List<MerchandiseReturnReasonModel>();
            AvailableReturnActions = new List<MerchandiseReturnActionModel>();
            ExistingAddresses = new List<AddressModel>();
            NewAddress = new AddressModel();
        }

        public string OrderId { get; set; }
        public int OrderNumber { get; set; }
        public string OrderCode { get; set; }
        public IList<OrderItemModel> Items { get; set; }
        
        public IList<MerchandiseReturnReasonModel> AvailableReturnReasons { get; set; }

        public IList<MerchandiseReturnActionModel> AvailableReturnActions { get; set; }

        [GrandResourceDisplayName("MerchandiseReturns.Comments")]
        public string Comments { get; set; }

        public DateTime? PickupDate { get; set; }

        public string Result { get; set; }

        public string Error { get; set; }

        public IList<AddressModel> ExistingAddresses { get; set; }

        public bool NewAddressPreselected { get; set; }

        public AddressModel NewAddress { get; set; }

        public bool ShowPickupAddress { get; set; }

        public bool ShowPickupDate { get; set; }

        public bool PickupDateRequired { get; set; }

        #region Nested classes

        public partial class OrderItemModel : BaseEntityModel
        {
            public string ProductId { get; set; }

            public string ProductName { get; set; }

            public string VendorId { get; set; }
            public string VendorName { get; set; }

            public string ProductSeName { get; set; }

            public string AttributeInfo { get; set; }

            public string UnitPrice { get; set; }

            public int Quantity { get; set; }

            [GrandResourceDisplayName("MerchandiseReturns.ReturnReason")]
            public string MerchandiseReturnReasonId { get; set; }

            [GrandResourceDisplayName("MerchandiseReturns.ReturnAction")]
            public string MerchandiseReturnActionId { get; set; }
        }

        public partial class MerchandiseReturnReasonModel : BaseEntityModel
        {
            public string Name { get; set; }
        }
        public partial class MerchandiseReturnActionModel : BaseEntityModel
        {
            public string Name { get; set; }
        }

        #endregion
    }

}