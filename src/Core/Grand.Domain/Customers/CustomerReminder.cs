namespace Grand.Domain.Customers
{
    /// <summary>
    /// Represents a customer reminder 
    /// </summary>
    public partial class CustomerReminder : BaseEntity
    {
        private ICollection<ReminderCondition> _condition;

        private ICollection<ReminderLevel> _level;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        public DateTime StartDateTimeUtc { get; set; }
        public DateTime EndDateTimeUtc { get; set; }
        public DateTime LastUpdateDate { get; set; }

        public bool AllowRenew { get; set; }
        public int RenewedDay { get; set; }
        /// <summary>
        /// Gets or sets display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets active action
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the action conditions
        /// </summary>
        public CustomerReminderConditionEnum ConditionId { get; set; }

        /// <summary>
        /// Gets or sets the reminder rule
        /// </summary>
        public CustomerReminderRuleEnum ReminderRuleId { get; set; }

        /// <summary>
        /// Gets or sets the customer condition
        /// </summary>
        public virtual ICollection<ReminderCondition> Conditions
        {
            get { return _condition ??= new List<ReminderCondition>(); }
            protected set { _condition = value; }
        }

        /// <summary>
        /// Gets or sets the reminder level
        /// </summary>
        public virtual ICollection<ReminderLevel> Levels
        {
            get { return _level ??= new List<ReminderLevel>(); }
            protected set { _level = value; }
        }


        public partial class ReminderCondition: SubBaseEntity
        {
            private ICollection<string> _products;
            private ICollection<string> _categories;
            private ICollection<string> _collections;
            private ICollection<string> _customerGroups;
            private ICollection<string> _customerTags;
            private ICollection<CustomerRegister> _customerRegister;
            private ICollection<CustomerRegister> _customCustomerAttributes;

            public string Name { get; set; }

            public CustomerReminderConditionTypeEnum ConditionTypeId { get; set; }

            public CustomerReminderConditionEnum ConditionId { get; set; }

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

            public partial class CustomerRegister: SubBaseEntity
            {
                public string RegisterField { get; set; }
                public string RegisterValue { get; set; }
            }

        }

        public partial class ReminderLevel: SubBaseEntity
        {
            public string Name { get; set; }
            public int Level { get; set; }
            public int Day { get; set; }
            public int Hour { get; set; }
            public int Minutes { get; set; }
            public string EmailAccountId { get; set; }
            public string BccEmailAddresses { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
        }
       
    }
}
