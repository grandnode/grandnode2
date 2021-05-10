using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Shipping
{
    public partial class ShippingMethodModel : BaseEntityModel, ILocalizedModel<ShippingMethodLocalizedModel>
    {
        public ShippingMethodModel()
        {
            Locales = new List<ShippingMethodLocalizedModel>();
        }
        [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<ShippingMethodLocalizedModel> Locales { get; set; }
    }

    public partial class ShippingMethodLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Description")]

        public string Description { get; set; }

    }
}