using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class NeverSoldReportModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Reports.NeverSold.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Admin.Reports.NeverSold.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }
    }
}