using System;
using System.Collections.Generic;

namespace Grand.Domain.Customers
{
    /// <summary>
    /// Represents a customer action
    /// </summary>
    public partial class CustomerAction : BaseEntity
    {
        private ICollection<ActionCondition> _condition;
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets active action
        /// </summary>
        public bool Active { get; set; }


        /// <summary>
        /// Gets or sets the action Type
        /// </summary>
        public string ActionTypeId { get; set; }

        /// <summary>
        /// Gets or sets the action conditions
        /// </summary>
        public CustomerActionConditionEnum ConditionId { get; set; }

        /// <summary>
        /// Gets or sets the action conditions
        /// </summary>
        public CustomerReactionTypeEnum ReactionTypeId { get; set; }

        public string BannerId { get; set; }
        public string InteractiveFormId { get; set; }

        public string MessageTemplateId { get; set; }

        public string CustomerGroupId { get; set; }

        public string CustomerTagId { get; set; }
        /// <summary>
        /// Gets or sets the start date 
        /// </summary>
        public DateTime StartDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the end date
        /// </summary>
        public DateTime EndDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the customer groups
        /// </summary>
        public virtual ICollection<ActionCondition> Conditions
        {
            get { return _condition ??= new List<ActionCondition>(); }
            protected set { _condition = value; }
        }

        public partial class ActionCondition: SubBaseEntity
        {
            private ICollection<string> _products;
            private ICollection<string> _categories;
            private ICollection<string> _collections;
            private ICollection<string> _vendors;
            private ICollection<string> _customerGroups;
            private ICollection<string> _customerTags;
            private ICollection<string> _stores;
            private ICollection<ProductAttributeValue> _productAttribute;
            private ICollection<ProductSpecification> _productSpecification;
            private ICollection<CustomerRegister> _customerRegister;
            private ICollection<CustomerRegister> _customCustomerAttributes;
            private ICollection<Url> _urlReferrer;
            private ICollection<Url> _urlCurrent;

            public string Name { get; set; }

            public CustomerActionConditionTypeEnum CustomerActionConditionTypeId { get; set; }

            public CustomerActionConditionEnum ConditionId { get; set; }

            public virtual ICollection<string> Products
            {
                get { return _products ??= new List<string>(); }
                protected set { _products = value; }
            }

            public virtual ICollection<string> Categories
            {
                get { return _categories ??= new List<string>(); }
                protected set { _categories = value; }
            }

            public virtual ICollection<string> Collections
            {
                get { return _collections ??= new List<string>(); }
                protected set { _collections = value; }
            }

            public virtual ICollection<string> Vendors
            {
                get { return _vendors ??= new List<string>(); }
                protected set { _vendors = value; }
            }
            public virtual ICollection<string> CustomerGroups
            {
                get { return _customerGroups ??= new List<string>(); }
                protected set { _customerGroups = value; }
            }
            public virtual ICollection<string> CustomerTags
            {
                get { return _customerTags ??= new List<string>(); }
                protected set { _customerTags = value; }
            }
            public virtual ICollection<string> Stores
            {
                get { return _stores ??= new List<string>(); }
                protected set { _stores = value; }
            }
            public virtual ICollection<ProductAttributeValue> ProductAttribute
            {
                get { return _productAttribute ??= new List<ProductAttributeValue>(); }
                protected set { _productAttribute = value; }
            }

            public virtual ICollection<ProductSpecification> ProductSpecifications
            {
                get { return _productSpecification ??= new List<ProductSpecification>(); }
                protected set { _productSpecification = value; }
            }

            public virtual ICollection<CustomerRegister> CustomerRegistration
            {
                get { return _customerRegister ??= new List<CustomerRegister>(); }
                protected set { _customerRegister = value; }
            }

            public virtual ICollection<CustomerRegister> CustomCustomerAttributes
            {
                get { return _customCustomerAttributes ??= new List<CustomerRegister>(); }
                protected set { _customCustomerAttributes = value; }
            }

            public virtual ICollection<Url> UrlReferrer
            {
                get { return _urlReferrer ??= new List<Url>(); }
                protected set { _urlReferrer = value; }
            }

            public virtual ICollection<Url> UrlCurrent
            {
                get { return _urlCurrent ??= new List<Url>(); }
                protected set { _urlCurrent = value; }
            }

            public partial class ProductAttributeValue: SubBaseEntity
            {
                public string ProductAttributeId { get; set; }
                public string Name { get; set; }
            }

            public partial class Url : SubBaseEntity
            {
                public string Name { get; set; }
            }

            public partial class ProductSpecification : SubBaseEntity
            {
                public string ProductSpecyficationId { get; set; }
                public string ProductSpecyficationValueId { get; set; }
            }

            public partial class CustomerRegister : SubBaseEntity
            {
                public string RegisterField { get; set; }
                public string RegisterValue { get; set; }
            }

        }

    }
}
