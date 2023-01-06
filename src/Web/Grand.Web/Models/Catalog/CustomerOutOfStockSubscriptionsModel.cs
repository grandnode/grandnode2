﻿using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Catalog
{
    public class CustomerOutOfStockSubscriptionsModel
    {
        public CustomerOutOfStockSubscriptionsModel()
        {
            Subscriptions = new List<OutOfStockSubscriptionModel>();
            PagerModel = new PagerModel();
        }

        public IList<OutOfStockSubscriptionModel> Subscriptions { get; set; }
        public PagerModel PagerModel { get; set; }

        #region Nested classes

        public class OutOfStockSubscriptionModel : BaseEntityModel
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string AttributeDescription { get; set; }
            public string SeName { get; set; }
        }

        #endregion
    }
}