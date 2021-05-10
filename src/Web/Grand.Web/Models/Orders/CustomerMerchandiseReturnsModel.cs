using Grand.Infrastructure.Models;
using System;
using System.Collections.Generic;

namespace Grand.Web.Models.Orders
{
    public partial class CustomerMerchandiseReturnsModel : BaseModel
    {
        public CustomerMerchandiseReturnsModel()
        {
            Items = new List<MerchandiseReturnModel>();
        }

        public IList<MerchandiseReturnModel> Items { get; set; }

        #region Nested classes
        public partial class MerchandiseReturnModel : BaseEntityModel
        {
            public int ReturnNumber { get; set; }
            public string MerchandiseReturnStatus { get; set; }
            public DateTime CreatedOn { get; set; }
            public int ProductsCount { get; set; }
            public string ReturnTotal { get; set; }
        }
        #endregion
    }
}