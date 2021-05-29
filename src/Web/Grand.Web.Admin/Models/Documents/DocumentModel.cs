using Grand.Web.Common.Link;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Documents
{
    public class DocumentModel : BaseEntityModel, IGroupLinkModel, IStoreLinkModel
    {
        public DocumentModel()
        {
            AvailableDocumentTypes = new List<SelectListItem>();
            AvailableSelesEmployees = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Number")]
        public string Number { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.SeId")]
        public string SeId { get; set; }
        public IList<SelectListItem> AvailableSelesEmployees { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Description")]
        public string Description { get; set; }

        public string ParentDocumentId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Picture")]
        [UIHint("Picture")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Download")]
        [UIHint("Download")]
        public string DownloadId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Flag")]
        public string Flag { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Link")]
        public string Link { get; set; }

        public string CustomerId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Status")]
        public int StatusId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Reference")]
        public int ReferenceId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Object")]
        public string ObjectId { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.DocumentType")]
        public string DocumentTypeId { get; set; }
        public IList<SelectListItem> AvailableDocumentTypes { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.CustomerEmail")]
        public string CustomerEmail { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Username")]
        public string Username { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.TotalAmount")]
        public double TotalAmount { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.OutstandAmount")]
        public double OutstandAmount { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.Quantity")]
        public int Quantity { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.DocDate")]
        [UIHint("DateTimeNullable")]
        public DateTime? DocDate { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Document.Fields.DueDate")]
        [UIHint("DateTimeNullable")]
        public DateTime? DueDate { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Documents.Document.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }
        
        //Store acl
        [GrandResourceDisplayName("Admin.Documents.Document.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }
    }
}
