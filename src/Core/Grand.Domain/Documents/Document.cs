﻿using Grand.Domain.Common;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;

namespace Grand.Domain.Documents
{
    /// <summary>
    /// Represents a document
    /// </summary>
    public partial class Document : BaseEntity, IGroupLinkEntity, IStoreLinkEntity
    {
        public Document()
        {
            CustomerGroups = new List<string>();
            Stores = new List<string>();
        }

        /// <summary>
        /// Gets or sets the document number
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sales employee identifier 
        /// </summary>
        public string SeId { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the parent document identifier
        /// </summary>
        public string ParentDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the picture identifier
        /// </summary>
        public string PictureId { get; set; }

        /// <summary>
        /// Gets or sets the download identifier
        /// </summary>
        public string DownloadId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the flag
        /// </summary>
        public string Flag { get; set; }

        /// <summary>
        /// Gets or sets the lik
        /// </summary>
        public string Link { get; set; }

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
        /// Gets or sets the customer identifier
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Gets or sets an document status identifier
        /// </summary>
        public DocumentStatus StatusId { get; set; }

        /// <summary>
        /// Gets or sets an document reference identifier
        /// </summary>
        public Reference ReferenceId { get; set; }

        /// <summary>
        /// Gets or sets an object reference identifier
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets an document type identifier (DocumentType)
        /// </summary>
        public string DocumentTypeId { get; set; }

        /// <summary>
        /// Gets or sets the customer email
        /// </summary>
        public string CustomerEmail { get; set; }

        /// <summary>
        /// Gets or sets the user name
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the total amount
        /// </summary>
        public double TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the outstand amount
        /// </summary>
        public double OutstandAmount { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the date and time of document creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of document update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of document date
        /// </summary>
        public DateTime? DocDate { get; set; }

        /// Gets or sets the date and time of document due date
        /// </summary>
        public DateTime? DueDate { get; set; }

    }
}
