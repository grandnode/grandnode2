using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class MerchandiseReturnListModel : BaseModel
    {
        public MerchandiseReturnListModel()
        {
            MerchandiseReturnStatus = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchCustomerEmail")]
        public string SearchCustomerEmail { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchMerchandiseReturnStatus")]
        public int SearchMerchandiseReturnStatusId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.GoDirectlyToId")]
        public string GoDirectlyToId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        public string StoreId { get; set; }

        public IList<SelectListItem> MerchandiseReturnStatus { get; set; }
    }
}