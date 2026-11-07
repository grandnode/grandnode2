using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Home;

public class DashboardModel : BaseModel;

public class DashboardActivityModel : BaseModel
{
    public int OrdersPending { get; set; }
    public int AbandonedCarts { get; set; }
    public int LowStockProducts { get; set; }

    /// <summary>
    ///     Batches holding stock that are expiring, and expired
    /// </summary>
    public int ExpiringBatches { get; set; }

    public int ExpiredBatches { get; set; }
    public int TodayRegisteredCustomers { get; set; }
    public int MerchandiseReturns { get; set; }
}