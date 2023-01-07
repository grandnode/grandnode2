﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Common
{
    public class FooterModel : BaseModel
    {
        public FooterModel()
        {
            Pages = new List<FooterPageModel>();
        }

        public string StoreName { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyHours { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhone { get; set; }

        public string FacebookLink { get; set; }
        public string TwitterLink { get; set; }
        public string YoutubeLink { get; set; }
        public string InstagramLink { get; set; }
        public string LinkedInLink { get; set; }
        public string PinterestLink { get; set; }
        public bool PrivacyPreference { get; set; }
        public bool WishlistEnabled { get; set; }
        public bool ShoppingCartEnabled { get; set; }
        public bool SitemapEnabled { get; set; }        
        public bool BlogEnabled { get; set; }
        public bool NewsEnabled { get; set; }
        public bool RecentlyViewedProductsEnabled { get; set; }
        public bool RecommendedProductsEnabled { get; set; }
        public bool NewProductsEnabled { get; set; }
        public bool AllowCustomersToApplyForVendorAccount { get; set; }
        public bool InclTax { get; set; }
        public bool KnowledgebaseEnabled { get; set; }

        public string WorkingLanguageId { get; set; }

        public IList<FooterPageModel> Pages { get; set; }

        #region Nested classes

        public class FooterPageModel : BaseEntityModel
        {
            public string Name { get; set; }
            public string SeName { get; set; }

            public bool IncludeInFooterRow1 { get; set; }
            public bool IncludeInFooterRow2 { get; set; }
            public bool IncludeInFooterRow3 { get; set; }
        }

        #endregion
    }
}