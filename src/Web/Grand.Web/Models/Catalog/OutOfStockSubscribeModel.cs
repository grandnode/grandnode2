using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog
{
    public partial class OutOfStockSubscribeModel : BaseModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSeName { get; set; }

        public bool IsCurrentCustomerRegistered { get; set; }
        public bool SubscriptionAllowed { get; set; }
        public bool AlreadySubscribed { get; set; }

        public int MaximumOutOfStockSubscriptions { get; set; }
        public int CurrentNumberOfOutOfStockSubscriptions { get; set; }
    }
}