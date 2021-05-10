using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;
using System.Collections.Generic;

namespace Grand.Web.Models.Orders
{
    public partial class CustomerLoyaltyPointsModel : BaseModel
    {
        public CustomerLoyaltyPointsModel()
        {
            LoyaltyPoints = new List<LoyaltyPointsHistoryModel>();
        }

        public IList<LoyaltyPointsHistoryModel> LoyaltyPoints { get; set; }
        public int LoyaltyPointsBalance { get; set; }
        public string LoyaltyPointsAmount { get; set; }
        public int MinimumLoyaltyPointsBalance { get; set; }
        public string MinimumLoyaltyPointsAmount { get; set; }

        #region Nested classes

        public partial class LoyaltyPointsHistoryModel : BaseEntityModel
        {
            [GrandResourceDisplayName("LoyaltyPoints.Fields.Points")]
            public int Points { get; set; }

            [GrandResourceDisplayName("LoyaltyPoints.Fields.PointsBalance")]
            public int PointsBalance { get; set; }

            [GrandResourceDisplayName("LoyaltyPoints.Fields.Message")]
            public string Message { get; set; }

            [GrandResourceDisplayName("LoyaltyPoints.Fields.Date")]
            public DateTime CreatedOn { get; set; }
        }

        #endregion
    }
}