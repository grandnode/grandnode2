using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Orders;

public class MerchandiseReturnModel : BaseModel
{
    public string OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string OrderCode { get; set; }
    public IList<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();

    public IList<MerchandiseReturnReasonModel> AvailableReturnReasons { get; set; } =
        new List<MerchandiseReturnReasonModel>();

    public IList<MerchandiseReturnActionModel> AvailableReturnActions { get; set; } =
        new List<MerchandiseReturnActionModel>();

    [GrandResourceDisplayName("MerchandiseReturns.Comments")]
    public string Comments { get; set; }

    public DateTime? PickupDate { get; set; }

    public string Result { get; set; }

    public string Error { get; set; }

    public IList<AddressModel> ExistingAddresses { get; set; } = new List<AddressModel>();

    public bool NewAddressPreselected { get; set; }

    public AddressModel MerchandiseReturnNewAddress { get; set; } = new();
    public string PickupAddressId { get; set; }
    public bool ShowPickupAddress { get; set; }

    public bool ShowPickupDate { get; set; }

    public bool PickupDateRequired { get; set; }

    /// <summary>
    ///     Whether the form leaves out the requested action; it is optional when shown
    /// </summary>
    public bool HideRequestedAction { get; set; }

    /// <summary>
    ///     Whether a return method is active for the store and the customer; the options are loaded once a quantity
    ///     is chosen
    /// </summary>
    public bool ReturnShippingEnabled { get; set; }

    /// <summary>
    ///     The return method chosen: the system name of its provider
    /// </summary>
    public string ReturnShippingProvider { get; set; }

    /// <summary>
    ///     The return method chosen: the code of the provider's option
    /// </summary>
    public string ReturnShippingOption { get; set; }

    #region Nested classes

    public class OrderItemModel : BaseEntityModel
    {
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string VendorId { get; set; }
        public string VendorName { get; set; }

        public string ProductSeName { get; set; }

        public string AttributeInfo { get; set; }

        public string UnitPrice { get; set; }
        public bool IncludingTax { get; set; }
        public int Quantity { get; set; }

        [GrandResourceDisplayName("MerchandiseReturns.ReturnReason")]
        public string MerchandiseReturnReasonId { get; set; }

        [GrandResourceDisplayName("MerchandiseReturns.ReturnAction")]
        public string MerchandiseReturnActionId { get; set; }
    }

    public class MerchandiseReturnReasonModel : BaseEntityModel
    {
        public string Name { get; set; }

        /// <summary>
        ///     Whether an item returned for the reason needs a photo
        /// </summary>
        public bool PhotoRequired { get; set; }
    }

    public class MerchandiseReturnActionModel : BaseEntityModel
    {
        public string Name { get; set; }
    }

    #endregion
}