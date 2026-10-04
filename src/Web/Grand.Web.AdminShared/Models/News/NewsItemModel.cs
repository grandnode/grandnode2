using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.News;

public class NewsItemModel : BaseEntityModel, ILocalizedModel<NewsLocalizedModel>, IGroupLinkModel, IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Title")]
    public string Title { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Picture")]
    public string PictureId { get; set; }

    [SanitizeHtml]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Short")]
    public string Short { get; set; }

    [SanitizeHtml]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Full")]
    public string Full { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.AllowComments")]
    public bool AllowComments { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.StartDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.EndDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? EndDate { get; set; }

    [NoHtml]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.MetaKeywords")]
    public string MetaKeywords { get; set; }

    [NoHtml]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.MetaDescription")]
    public string MetaDescription { get; set; }

    [NoHtml]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.MetaTitle")]
    public string MetaTitle { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.SeName")]
    public string SeName { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Published")]
    public bool Published { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Comments")]
    public int Comments { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    //ACL
    [UIHint("CustomerGroups")]
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.LimitedToGroups")]
    public string[] CustomerGroups { get; set; }

    public IList<NewsLocalizedModel> Locales { get; set; } = new List<NewsLocalizedModel>();

    //Store acl
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }
}

public class NewsLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
{
    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Title")]

    public string Title { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Short")]
    [SanitizeHtml]
    public string Short { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.Full")]
    [SanitizeHtml]
    public string Full { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.MetaKeywords")]
    [NoHtml]
    public string MetaKeywords { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.MetaDescription")]
    [NoHtml]
    public string MetaDescription { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.MetaTitle")]
    [NoHtml]
    public string MetaTitle { get; set; }

    public string LanguageId { get; set; }

    [GrandResourceDisplayName("Admin.Content.News.NewsItems.Fields.SeName")]

    public string SeName { get; set; }
}