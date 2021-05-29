using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;

namespace Grand.Web.Admin.Models.ShoppingCart
{
    public partial class ShoppingCartItemModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.CurrentCarts.Store")]
        public string Store { get; set; }
        [GrandResourceDisplayName("Admin.CurrentCarts.Product")]
        public string ProductId { get; set; }
        [GrandResourceDisplayName("Admin.CurrentCarts.Product")]
        public string ProductName { get; set; }
        public string AttributeInfo { get; set; }

        [GrandResourceDisplayName("Admin.CurrentCarts.UnitPrice")]
        public string UnitPrice { get; set; }
        public double UnitPriceValue { get; set; }
        [GrandResourceDisplayName("Admin.CurrentCarts.Quantity")]
        public int Quantity { get; set; }
        [GrandResourceDisplayName("Admin.CurrentCarts.Total")]
        public string Total { get; set; }
        [GrandResourceDisplayName("Admin.CurrentCarts.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }
    }
}