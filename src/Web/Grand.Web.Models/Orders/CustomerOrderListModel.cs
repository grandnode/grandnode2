using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

public class CustomerOrderListModel : BaseModel
{
    public OrderPagingModel PagingContext { get; set; } = new();
    public IList<OrderDetailsModel> Orders { get; set; } = new List<OrderDetailsModel>();

    /// <summary>
    ///     Gets or sets the trade credit of the customer's company; null when it does not buy on credit
    /// </summary>
    public TradeCreditModel TradeCredit { get; set; }

    #region Nested classes

    public class TradeCreditModel : BaseModel
    {
        public string CreditLimit { get; set; }
        public string Outstanding { get; set; }
        public string Available { get; set; }
        public int PaymentTermDays { get; set; }
        public bool HasOverdue { get; set; }
    }

    public class OrderDetailsModel : BaseEntityModel
    {
        public string OrderTotal { get; set; }
        public bool IsMerchandiseReturnAllowed { get; set; }
        public int OrderStatusId { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string ShippingStatus { get; set; }
        public DateTime CreatedOn { get; set; }
        public int OrderNumber { get; set; }
        public string OrderCode { get; set; }
        public string CustomerEmail { get; set; }
    }

    #endregion
}