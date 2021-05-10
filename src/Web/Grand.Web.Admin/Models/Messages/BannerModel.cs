using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class BannerModel : BaseEntityModel, ILocalizedModel<BannerLocalizedModel>
    {
        public BannerModel()
        {
            Locales = new List<BannerLocalizedModel>();
        }

        [GrandResourceDisplayName("admin.marketing.Banners.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.Banners.Fields.Body")]

        public string Body { get; set; }

        public IList<BannerLocalizedModel> Locales { get; set; }

    }

    public partial class BannerLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("admin.marketing.Banners.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.Banners.Fields.Body")]

        public string Body { get; set; }

    }

}