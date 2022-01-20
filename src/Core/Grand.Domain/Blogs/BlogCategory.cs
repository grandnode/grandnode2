﻿using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Domain.Blogs
{
    public partial class BlogCategory : BaseEntity, IStoreLinkEntity, ITranslationEntity
    {
        public BlogCategory()
        {
            Stores = new List<string>();
            Locales = new List<TranslationEntity>();
            BlogPosts = new List<BlogCategoryPost>();
        }

        /// <summary>
        /// Gets or sets the blog category name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the blog category sename
        /// </summary>
        public string SeName { get; set; }

        /// <summary>
        /// Gets or sets display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }
        public IList<string> Stores { get; set; }

        public IList<BlogCategoryPost> BlogPosts { get; set; }

    }
}
