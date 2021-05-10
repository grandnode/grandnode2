using System;
using System.Collections.Generic;

namespace Grand.Domain.Messages
{
    /// <summary>
    /// Represents a campaign
    /// </summary>
    public partial class Campaign : BaseEntity
    {
        private ICollection<string> _customerTags;
        private ICollection<string> _customerGroups;
        private ICollection<string> _newsletterCategories;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the lang identifier
        /// </summary>
        public string LanguageId { get; set; }

        public DateTime? CustomerCreatedDateFrom { get; set; }
        public DateTime? CustomerCreatedDateTo { get; set; }
        public DateTime? CustomerLastActivityDateFrom { get; set; }
        public DateTime? CustomerLastActivityDateTo { get; set; }
        public DateTime? CustomerLastPurchaseDateFrom { get; set; }
        public DateTime? CustomerLastPurchaseDateTo { get; set; }

        public CampaignCondition CustomerHasOrders { get; set; }
        
        public CampaignCondition CustomerHasShoppingCart { get; set; }
       
        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the used email account identifier
        /// </summary>
        public string EmailAccountId { get; set; }
        /// <summary>
        /// Gets or sets the customer tags
        /// </summary>
        public virtual ICollection<string> CustomerTags
        {
            get { return _customerTags ??= new List<string>(); }
            protected set { _customerTags = value; }
        }
        /// <summary>
        /// Gets or sets the customer groups
        /// </summary>
        public virtual ICollection<string> CustomerGroups
        {
            get { return _customerGroups ??= new List<string>(); }
            protected set { _customerGroups = value; }
        }
        /// <summary>
        /// Gets or sets the newsletter categories
        /// </summary>
        public virtual ICollection<string> NewsletterCategories
        {
            get { return _newsletterCategories ??= new List<string>(); }
            protected set { _newsletterCategories = value; }
        }

    }
}
