using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

public class CustomerMerchandiseReturnsModel : BaseModel
{
    public IList<MerchandiseReturnModel> Items { get; set; } = new List<MerchandiseReturnModel>();

    /// <summary>
    ///     The order a guest may still return items of; null shows no button
    /// </summary>
    public string ReturnOrderId { get; set; }

    /// <summary>
    ///     Shown to a guest who came with a return link, who has no account pages to go to
    /// </summary>
    public bool Guest { get; set; }

    #region Nested classes

    public class MerchandiseReturnModel : BaseEntityModel
    {
        public int ReturnNumber { get; set; }
        public string MerchandiseReturnStatus { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ProductsCount { get; set; }
        public string ReturnTotal { get; set; }

        /// <summary>
        ///     The name of the return method; empty when the return has none
        /// </summary>
        public string ReturnShippingMethod { get; set; }
    }

    #endregion
}