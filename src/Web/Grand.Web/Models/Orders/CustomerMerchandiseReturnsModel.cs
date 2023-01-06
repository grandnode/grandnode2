using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders
{
    public class CustomerMerchandiseReturnsModel : BaseModel
    {
        public CustomerMerchandiseReturnsModel()
        {
            Items = new List<MerchandiseReturnModel>();
        }

        public IList<MerchandiseReturnModel> Items { get; set; }

        #region Nested classes
        public class MerchandiseReturnModel : BaseEntityModel
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