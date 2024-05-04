using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages;

public class NewsLetterSubscriptionListModel : BaseModel
{
    [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchEmail")]
    public string SearchEmail { get; set; }

    [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchStore")]
    public string StoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchActive")]
    public int ActiveId { get; set; }

    [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchActive")]
    public IList<SelectListItem> ActiveList { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.Categories")]

    public IList<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.Category")]
    [UIHint("MultiSelect")]
    public IList<string> SearchCategoryIds { get; set; } = new List<string>();
}