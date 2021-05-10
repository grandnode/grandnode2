using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class NewsLetterSubscriptionListModel : BaseModel
    {
        public NewsLetterSubscriptionListModel()
        {
            AvailableStores = new List<SelectListItem>();
            ActiveList = new List<SelectListItem>();
            SearchCategoryIds = new List<string>();
            AvailableCategories = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchEmail")]
        public string SearchEmail { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchStore")]
        public string StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchActive")]
        public int ActiveId { get; set; }
        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.SearchActive")]
        public IList<SelectListItem> ActiveList { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.Categories")]
        
        public IList<SelectListItem> AvailableCategories { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.List.Category")]
        [UIHint("MultiSelect")]
        public IList<string> SearchCategoryIds { get; set; }

    }
}