using Grand.Web.Common.Localization;
using Grand.Web.Common.Link;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class NewsletterCategoryModel : BaseEntityModel, ILocalizedModel<NewsletterCategoryLocalizedModel>, IStoreLinkModel
    {
        public NewsletterCategoryModel()
        {
            Locales = new List<NewsletterCategoryLocalizedModel>();
        }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Selected")]
        public bool Selected { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }
        public IList<NewsletterCategoryLocalizedModel> Locales { get; set; }
    }

    public partial class NewsletterCategoryLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Description")]

        public string Description { get; set; }

    }
}