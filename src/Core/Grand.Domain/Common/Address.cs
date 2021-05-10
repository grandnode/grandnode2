using System;
using System.Collections.Generic;

namespace Grand.Domain.Common
{
    public partial class Address : BaseEntity
    {
        public Address()
        {
            Attributes = new List<CustomAttribute>();
        }

        /// <summary>
        /// Gets or sets the first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the company
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// Gets or sets the vat numer
        /// </summary>
        public string VatNumber { get; set; }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public string CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public string StateProvinceId { get; set; }
        
        /// <summary>
        /// Gets or sets the city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the address 1
        /// </summary>
        public string Address1 { get; set; }

        /// <summary>
        /// Gets or sets the address 2
        /// </summary>
        public string Address2 { get; set; }

        /// <summary>
        /// Gets or sets the zip/postal code
        /// </summary>
        public string ZipPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the fax number
        /// </summary>
        public string FaxNumber { get; set; }

        /// <summary>
        /// Gets or sets the note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the custom attributes (see "AddressAttribute" entity for more info)
        /// </summary>
        public IList<CustomAttribute> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the address type
        /// </summary>
        public AddressType AddressType { get; set; }
       
        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
    }
}
