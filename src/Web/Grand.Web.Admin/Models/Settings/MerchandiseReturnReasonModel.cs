using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class MerchandiseReturnReasonModel : BaseEntityModel, ILocalizedModel<MerchandiseReturnReasonLocalizedModel>
    {
        public MerchandiseReturnReasonModel()
        {
            Locales = new List<MerchandiseReturnReasonLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<MerchandiseReturnReasonLocalizedModel> Locales { get; set; }
    }

    public partial class MerchandiseReturnReasonLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.Name")]

        public string Name { get; set; }

    }
}