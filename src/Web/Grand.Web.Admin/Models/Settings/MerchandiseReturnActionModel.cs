using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class MerchandiseReturnActionModel : BaseEntityModel, ILocalizedModel<MerchandiseReturnActionLocalizedModel>
    {
        public MerchandiseReturnActionModel()
        {
            Locales = new List<MerchandiseReturnActionLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<MerchandiseReturnActionLocalizedModel> Locales { get; set; }
    }

    public partial class MerchandiseReturnActionLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.Name")]

        public string Name { get; set; }

    }
}