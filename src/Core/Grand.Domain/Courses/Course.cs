﻿using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Domain.Stores;

namespace Grand.Domain.Courses
{
    public class Course : BaseEntity, ITranslationEntity, ISlugEntity, IGroupLinkEntity, IStoreLinkEntity
    {
        public Course()
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
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the picture
        /// </summary>
        public string PictureId { get; set; }

        /// <summary>
        /// Gets or sets the product ident
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the level ident
        /// </summary>
        public string LevelId { get; set; }

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
        /// Gets or sets the name
        /// </summary>
        public string SeName { get; set; }

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

    }
}
