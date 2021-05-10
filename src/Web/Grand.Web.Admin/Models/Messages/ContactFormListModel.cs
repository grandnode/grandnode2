using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class ContactFormListModel : BaseModel
    {
        public ContactFormListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }
        [GrandResourceDisplayName("Admin.System.ContactForm.List.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? SearchStartDate { get; set; }

        [GrandResourceDisplayName("Admin.System.ContactForm.List.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? SearchEndDate { get; set; }

        [GrandResourceDisplayName("Admin.System.ContactForm.List.Email")]
        
        public string SearchEmail { get; set; }

        [GrandResourceDisplayName("Admin.System.ContactForm.List.Store")]
        public string StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

    }
}