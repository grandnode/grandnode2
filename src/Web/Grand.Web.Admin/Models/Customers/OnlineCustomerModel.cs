using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class OnlineCustomerModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Dashboards.OnlineCustomers.Fields.CustomerInfo")]
        public string CustomerInfo { get; set; }

        [GrandResourceDisplayName("Admin.Dashboards.OnlineCustomers.Fields.IPAddress")]
        public string LastIpAddress { get; set; }

        [GrandResourceDisplayName("Admin.Dashboards.OnlineCustomers.Fields.Location")]
        public string Location { get; set; }

        [GrandResourceDisplayName("Admin.Dashboards.OnlineCustomers.Fields.LastActivityDate")]
        public DateTime LastActivityDate { get; set; }
        
        [GrandResourceDisplayName("Admin.Dashboards.OnlineCustomers.Fields.LastVisitedPage")]
        public string LastVisitedPage { get; set; }
    }
}