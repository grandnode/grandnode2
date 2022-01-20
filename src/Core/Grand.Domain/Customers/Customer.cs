using Grand.Domain.Common;
using Grand.Domain.Orders;

namespace Grand.Domain.Customers
{
    /// <summary>
    /// Represents a customer
    /// </summary>
    public partial class Customer : BaseEntity
    {
        private ICollection<ShoppingCartItem> _shoppingCartItems;
        private ICollection<Address> _addresses;
        private ICollection<string> _groups;
        private ICollection<string> _customerTags;

        /// <summary>
        /// Ctor
        /// </summary>
        public Customer()
        {
            CustomerGuid = Guid.NewGuid();
            PasswordFormatId = PasswordFormat.Clear;
            Attributes = new List<CustomAttribute>();
        }

        /// <summary>
        /// Gets or sets the customer Guid
        /// </summary>
        public Guid CustomerGuid { get; set; }

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets the email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Gets or sets the password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password format
        /// </summary>
        public PasswordFormat PasswordFormatId { get; set; }
        
        /// <summary>
        /// Gets or sets the password salt
        /// </summary>
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer is tax exempt
        /// </summary>
        public bool IsTaxExempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer has a free shipping to the next a order
        /// </summary>
        public bool FreeShipping { get; set; }

        /// <summary>
        /// Gets or sets the affiliate identifier
        /// </summary>
        public string AffiliateId { get; set; }

        /// <summary>
        /// Gets or sets the vendor identifier with which this customer is associated (manager)
        /// </summary>
        public string VendorId { get; set; }

        /// <summary>
        /// Gets or sets the store identifier 
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the staff store identifier
        /// </summary>
        public string StaffStoreId { get; set; }

        /// <summary>
        /// Gets or sets the owner identifier
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the sales employee identifier 
        /// </summary>
        public string SeId { get; set; }

        /// <summary>
        /// Gets or sets the custom attributes (see "CustomerAttribute" entity for more info)
        /// </summary>
        public IList<CustomAttribute> Attributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer account is system
        /// </summary>
        public bool IsSystemAccount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer is active by adding comments etc...
        /// </summary>
        public bool HasContributions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating number of failed login attempts (wrong password)
        /// </summary>
        public int FailedLoginAttempts { get; set; }

        /// <summary>
        /// Gets or sets the date and time until which a customer cannot login (locked out)
        /// </summary>
        public DateTime? CannotLoginUntilDateUtc { get; set; }
        /// <summary>
        /// Gets or sets the customer system name
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the last IP address
        /// </summary>
        public string LastIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last login
        /// </summary>
        public DateTime? LastLoginDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last activity
        /// </summary>
        public DateTime LastActivityDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last purchase
        /// </summary>
        public DateTime? LastPurchaseDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last update cart
        /// </summary>
        public DateTime? LastUpdateCartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last update wishlist
        /// </summary>
        public DateTime? LastUpdateWishListDateUtc { get; set; }

        /// <summary>
        /// Last date to change password
        /// </summary>
        public DateTime? PasswordChangeDateUtc { get; set; }

        #region Navigation properties
        
        /// <summary>
        /// Gets or sets the customer groups
        /// </summary>
        public virtual ICollection<string> Groups
        {
            get { return _groups ??= new List<string>(); }
            protected set { _groups = value; }
        }

        /// <summary>
        /// Gets or sets shopping cart items
        /// </summary>
        public virtual ICollection<ShoppingCartItem> ShoppingCartItems
        {
            get { return _shoppingCartItems ??= new List<ShoppingCartItem>(); }
            protected set { _shoppingCartItems = value; }            
        }

        /// <summary>
        /// Default billing address
        /// </summary>
        public virtual Address BillingAddress { get; set; }

        /// <summary>
        /// Default shipping address
        /// </summary>
        public virtual Address ShippingAddress { get; set; }

        /// <summary>
        /// Gets or sets the coordinates
        /// </summary>
        public GeoCoordinates Coordinates { get; set; }
        
        /// <summary>
        /// Gets or sets customer addresses
        /// </summary>
        public virtual ICollection<Address> Addresses
        {
            get { return _addresses ??= new List<Address>(); }
            protected set { _addresses = value; }            
        }

        /// <summary>
        /// Gets or sets the customer tags
        /// </summary>
        public virtual ICollection<string> CustomerTags
        {
            get { return _customerTags ??= new List<string>(); }
            protected set { _customerTags = value; }
        }
        #endregion
    }
}