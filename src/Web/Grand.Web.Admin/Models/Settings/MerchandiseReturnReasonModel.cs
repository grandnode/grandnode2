using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public class MerchandiseReturnReasonModel : BaseEntityModel, ILocalizedModel<MerchandiseReturnReasonLocalizedModel>
    {
        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<MerchandiseReturnReasonLocalizedModel> Locales { get; set; } = new List<MerchandiseReturnReasonLocalizedModel>();
    }

    public class MerchandiseReturnReasonLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.Name")]

        public string Name { get; set; }

    }
}