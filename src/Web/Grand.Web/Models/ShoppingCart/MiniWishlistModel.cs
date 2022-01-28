using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.ShoppingCart
{
    public partial class MiniWishlistModel : BaseModel
    {
        public MiniWishlistModel()
        {
            Items = new List<WishlistItemModel>();
        }

        public IList<WishlistItemModel> Items { get; set; }
        public int TotalProducts { get; set; }
        public bool ShowProductImages { get; set; }

        public bool EmailWishlistEnabled { get; set; }

        #region Nested Classes

        public partial class WishlistItemModel : BaseEntityModel
        {
            public WishlistItemModel()
            {
                Picture = new PictureModel();
            }

            public string ProductId { get; set; }

            public string ProductName { get; set; }

            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public string Sku { get; set; }

            public int Quantity { get; set; }

            public string UnitPrice { get; set; }
            public double UnitPriceValue { get; set; }
            public double TaxRate { get; set; }

            public string AttributeInfo { get; set; }

            public PictureModel Picture { get; set; }
        }

        #endregion
    }
}