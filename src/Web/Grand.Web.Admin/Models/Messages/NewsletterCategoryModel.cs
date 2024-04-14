using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages;

public class NewsletterCategoryModel : BaseEntityModel, ILocalizedModel<NewsletterCategoryLocalizedModel>,
    IStoreLinkModel
{
    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Description")]

    public string Description { get; set; }

    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Selected")]
    public bool Selected { get; set; }

    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<NewsletterCategoryLocalizedModel> Locales { get; set; } = new List<NewsletterCategoryLocalizedModel>();

    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }
}

public class NewsletterCategoryLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("admin.marketing.NewsletterCategory.Fields.Description")]

    public string Description { get; set; }

    public string LanguageId { get; set; }
}