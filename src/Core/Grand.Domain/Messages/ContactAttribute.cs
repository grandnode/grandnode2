using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;

namespace Grand.Domain.Messages
{
    /// <summary>
    /// Represents a contact attribute
    /// </summary>
    public partial class ContactAttribute : BaseEntity, ITranslationEntity, IStoreLinkEntity, IGroupLinkEntity
    {
        private ICollection<ContactAttributeValue> _contactAttributeValues;

        public ContactAttribute()
        {
            Stores = new List<string>();
            Locales = new List<TranslationEntity>();
            CustomerGroups = new List<string>();
            ConditionAttribute = new List<CustomAttribute>(); 
        }
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text prompt
        /// </summary>
        public string TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is required
        /// </summary>
        public bool IsRequired { get; set; }


        /// <summary>
        /// Gets or sets the attribute control type identifier
        /// </summary>
        public int AttributeControlTypeId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }
        public IList<string> Stores { get; set; }


        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

        //validation fields

        /// <summary>
        /// Gets or sets the validation rule for minimum length (for textbox and multiline textbox)
        /// </summary>
        public int? ValidationMinLength { get; set; }

        /// <summary>
        /// Gets or sets the validation rule for maximum length (for textbox and multiline textbox)
        /// </summary>
        public int? ValidationMaxLength { get; set; }

        /// <summary>
        /// Gets or sets the validation rule for file allowed extensions (for file upload)
        /// </summary>
        public string ValidationFileAllowedExtensions { get; set; }

        /// <summary>
        /// Gets or sets the validation rule for file maximum size in kilobytes (for file upload)
        /// </summary>
        public int? ValidationFileMaximumSize { get; set; }

        /// <summary>
        /// Gets or sets the default value (for textbox and multiline textbox)
        /// </summary>
        public string DefaultValue { get; set; }

        public IList<CustomAttribute> ConditionAttribute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL
        /// </summary>
        public bool LimitedToGroups { get; set; }
        public IList<string> CustomerGroups { get; set; }



        /// <summary>
        /// Gets the attribute control type
        /// </summary>
        public AttributeControlType AttributeControlType
        {
            get
            {
                return (AttributeControlType)AttributeControlTypeId;
            }
            set
            {
                AttributeControlTypeId = (int)value;
            }
        }
        /// <summary>
        /// Gets the checkout attribute values
        /// </summary>
        public virtual ICollection<ContactAttributeValue> ContactAttributeValues
        {
            get { return _contactAttributeValues ??= new List<ContactAttributeValue>(); }
            protected set { _contactAttributeValues = value; }
        }
    }

}
