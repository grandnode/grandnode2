using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Pages
{
    /// <summary>
    /// Represents a page
    /// </summary>
    public partial class Page : BaseEntity, ITranslationEntity, ISlugEntity, IStoreLinkEntity, IGroupLinkEntity
    {
        public Page()
        {
            Locales = new List<TranslationEntity>();
            Stores = new List<string>();
            CustomerGroups = new List<string>();
        }
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string SystemName { get; set; }


        /// <summary>
        /// Gets or sets the sename
        /// </summary>
        public string SeName { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this page should be included in sitemap
        /// </summary>
        public bool IncludeInSitemap { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this page should be included in menu
        /// </summary>
        public bool IncludeInMenu { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this page should be included in footer (column 1)
        /// </summary>
        public bool IncludeInFooterRow1 { get; set; }
        /// <summary>
        /// Gets or sets the value indicating whether this page should be included in footer (column 1)
        /// </summary>
        public bool IncludeInFooterRow2 { get; set; }
        /// <summary>
        /// Gets or sets the value indicating whether this page should be included in footer (column 1)
        /// </summary>
        public bool IncludeInFooterRow3 { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this page is accessible when a store is closed
        /// </summary>
        public bool AccessibleWhenStoreClosed { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this page is password protected
        /// </summary>
        public bool IsPasswordProtected { get; set; }

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets a value of used page layout identifier
        /// </summary>
        public string PageLayoutId { get; set; }

        /// <summary>
        /// Gets or sets the meta keywords
        /// </summary>
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description
        /// </summary>
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title
        /// </summary>
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }
        public IList<string> Stores { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL
        /// </summary>
        public bool LimitedToGroups { get; set; }
        public IList<string> CustomerGroups { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }
        /// <summary>
        /// Gets or sets a start date of page
        /// </summary>
        public DateTime? StartDateUtc { get; set; }
        /// <summary>
        /// Gets or sets a end date of page
        /// </summary>
        public DateTime? EndDateUtc { get; set; }
    }
}
