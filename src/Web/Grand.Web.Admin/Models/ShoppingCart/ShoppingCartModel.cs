using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.ShoppingCart
{
    public partial class ShoppingCartModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.CurrentCarts.Customer")]
        public string CustomerId { get; set; }
        [GrandResourceDisplayName("Admin.CurrentCarts.Customer")]
        public string CustomerEmail { get; set; }

        [GrandResourceDisplayName("Admin.CurrentCarts.TotalItems")]
        public int TotalItems { get; set; }
    }
}