using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

public class OrderApprovalListModel : BaseModel
{
    public OrderPagingModel PagingContext { get; set; } = new();

    /// <summary>
    ///     The list shows the whole company's requests and not only the customer's own
    /// </summary>
    public bool IsCompanyList { get; set; }

    public IList<OrderApprovalRowModel> Approvals { get; set; } = new List<OrderApprovalRowModel>();

    public class OrderApprovalRowModel : BaseEntityModel
    {
        public DateTime CreatedOn { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public bool IsPending { get; set; }

        /// <summary>
        ///     The buyer's name or e-mail; filled for the company list
        /// </summary>
        public string Buyer { get; set; }

        public int ItemCount { get; set; }
        public string Total { get; set; }
    }
}
