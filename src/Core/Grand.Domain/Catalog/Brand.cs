using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a brand
    /// </summary>
    public partial class Brand : BaseEntity, ITranslationEntity, ISlugEntity, IGroupLinkEntity, IStoreLinkEntity
    {
        private ICollection<string> _appliedDiscounts;

        public Brand()
        {
            CustomerGroups = new List<string>();
            Stores = new List<string>();
            Locales = new List<TranslationEntity>();
        }
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sename
        /// </summary>
        public string SeName { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string BottomDescription { get; set; }

        /// <summary>
        /// Gets or sets a value of used brand layout identifier
        /// </summary>
        public string BrandLayoutId { get; set; }

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
        /// Gets or sets the picture identifier
        /// </summary>
        public string PictureId { get; set; }

        /// <summary>
        /// Gets or sets the page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether customers can select the page size
        /// </summary>
        public bool AllowCustomersToSelectPageSize { get; set; }

        /// <summary>
        /// Gets or sets the available customer selectable page size options
        /// </summary>
        public string PageSizeOptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the collection on home page
        /// </summary>
        public bool ShowOnHomePage { get; set; }
       
        /// <summary>
        /// Gets or sets a value indicating whether to include this collection in the menu
        /// </summary>
        public bool IncludeInMenu { get; set; }

        /// <summary>
        /// Gets or sets the Icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the Default sort
        /// </summary>
        public int DefaultSort { get; set; } = -1;

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL
 	    /// </summary>
        public bool LimitedToGroups { get; set; }
        public IList<string> CustomerGroups { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }
        public IList<string> Stores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

        /// <summary>
        /// Gets or sets the collection of applied discounts
        /// </summary>
        public virtual ICollection<string> AppliedDiscounts
        {
            get { return _appliedDiscounts ??= new List<string>(); }
            protected set { _appliedDiscounts = value; }
        }
    }
}
