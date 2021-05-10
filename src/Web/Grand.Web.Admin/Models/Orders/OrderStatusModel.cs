using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class OrderStatusModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Orders.OrderStatus.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Orders.OrderStatus.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

    }
}