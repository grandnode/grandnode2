using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Pages
{
    public partial class PageModel : BaseEntityModel, ILocalizedModel<PageLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public PageModel()
        {
            AvailablePageLayouts = new List<SelectListItem>();
            Locales = new List<PageLocalizedModel>();
        }

        //Store acl
        [GrandResourceDisplayName("Admin.Content.Pages.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }


        [GrandResourceDisplayName("Admin.Content.Pages.Fields.SystemName")]
        public string SystemName { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.IncludeInSitemap")]
        public bool IncludeInSitemap { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.IncludeInMenu")]
        public bool IncludeInMenu { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.IncludeInFooterRow1")]
        public bool IncludeInFooterRow1 { get; set; }
        [GrandResourceDisplayName("Admin.Content.Pages.Fields.IncludeInFooterRow2")]
        public bool IncludeInFooterRow2 { get; set; }
        [GrandResourceDisplayName("Admin.Content.Pages.Fields.IncludeInFooterRow3")]
        public bool IncludeInFooterRow3 { get; set; }
        [GrandResourceDisplayName("Admin.Content.Pages.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.AccessibleWhenStoreClosed")]
        public bool AccessibleWhenStoreClosed { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.IsPasswordProtected")]
        public bool IsPasswordProtected { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.URL")]

        public string Url { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.Title")]

        public string Title { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.Body")]

        public string Body { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.PageLayout")]
        public string PageLayoutId { get; set; }
        public IList<SelectListItem> AvailablePageLayouts { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.SeName")]

        public string SeName { get; set; }

        public IList<PageLocalizedModel> Locales { get; set; }
        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Content.Pages.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.StartDate")]
        [UIHint("DateTimeNullable")]
        public DateTime? StartDateUtc { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.EndDate")]
        [UIHint("DateTimeNullable")]
        public DateTime? EndDateUtc { get; set; }
    }

    public partial class PageLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.Title")]

        public string Title { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.Body")]

        public string Body { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.Fields.SeName")]

        public string SeName { get; set; }

    }
}