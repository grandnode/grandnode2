using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;
using System.Collections.Generic;

namespace Grand.Web.Models.Catalog
{
    public partial class CustomerOutOfStockSubscriptionsModel
    {
        public CustomerOutOfStockSubscriptionsModel()
        {
            Subscriptions = new List<OutOfStockSubscriptionModel>();
            PagerModel = new PagerModel();
        }

        public IList<OutOfStockSubscriptionModel> Subscriptions { get; set; }
        public PagerModel PagerModel { get; set; }

        #region Nested classes

        public partial class OutOfStockSubscriptionModel : BaseEntityModel
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string AttributeDescription { get; set; }
            public string SeName { get; set; }
        }

        #endregion
    }
}