using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Orders
{
    public class OrderTagModel : BaseEntityModel, ILocalizedModel<OrderTagLocalizedModel>
    {
        public OrderTagModel()
        {
            Locales = new List<OrderTagLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Orders.OrderTags.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Orders.OrderTags.Fields.OrderCount")]
        public int OrderCount { get; set; }
        public IList<OrderTagLocalizedModel> Locales { get; set; }
    }

    public partial class OrderTagLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }
        [GrandResourceDisplayName("Admin.Orders.OrderTags.Fields.Name")]
        public string Name { get; set; }

    }
}
