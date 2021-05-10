using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class PaymentTransactionListModel : BaseModel
    {
        public PaymentTransactionListModel()
        {
            PaymentTransactionStatus = new List<SelectListItem>();
            SearchTransactionStatus = -1;
        }

        [GrandResourceDisplayName("Admin.Orders.PaymentTransaction.List.SearchCustomerEmail")]
        public string SearchCustomerEmail { get; set; }

        [GrandResourceDisplayName("Admin.Orders.PaymentTransaction.List.SearchTransactionStatus")]
        public int SearchTransactionStatus { get; set; }

        [GrandResourceDisplayName("Admin.Orders.PaymentTransaction.List.OrderNumber")]
        public string OrderNumber { get; set; }

        [GrandResourceDisplayName("Admin.Orders.PaymentTransaction.List.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Admin.Orders.PaymentTransaction.List.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        public string StoreId { get; set; }

        public IList<SelectListItem> PaymentTransactionStatus { get; set; }
    }
}