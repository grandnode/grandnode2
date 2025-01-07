﻿using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.ShoppingCart;

public class MiniShoppingCartModel : BaseModel
{
    public IList<ShoppingCartItemModel> Items { get; set; } = new List<ShoppingCartItemModel>();
    public int TotalProducts { get; set; }
    public string SubTotal { get; set; }
    public bool SubTotalIncludingTax { get; set; }
    public bool DisplayShoppingCartButton { get; set; }
    public bool DisplayCheckoutButton { get; set; }
    public bool CurrentCustomerIsGuest { get; set; }
    public bool AnonymousCheckoutAllowed { get; set; }
    public bool ShowProductImages { get; set; }


    #region Nested Classes

    public class ShoppingCartItemModel : BaseEntityModel
    {
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductSeName { get; set; }
        public string ProductUrl { get; set; }

        public int Quantity { get; set; }

        public string UnitPrice { get; set; }

        public string AttributeInfo { get; set; }

        public PictureModel Picture { get; set; } = new();
    }

    #endregion
}