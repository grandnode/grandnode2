using Grand.Infrastructure.Models;
using System;

namespace Grand.Web.Models.Pages
{
    public partial class PageModel : BaseEntityModel
    {
        public string SystemName { get; set; }

        public bool IncludeInSitemap { get; set; }

        public bool IsPasswordProtected { get; set; }

        public string Password { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string MetaTitle { get; set; }

        public string SeName { get; set; }

        public string PageLayoutId { get; set; }

        public bool Published { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}