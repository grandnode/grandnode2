using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerListModel : BaseModel
    {
        public CustomerListModel()
        {
            AvailableCustomerTags = new List<SelectListItem>();
            SearchCustomerTagIds = new List<string>();
            SearchCustomerGroupIds = new List<string>();
            AvailableCustomerGroups = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.CustomerGroups")]
        
        public IList<SelectListItem> AvailableCustomerGroups { get; set; }


        [GrandResourceDisplayName("Admin.Customers.Customers.List.CustomerGroups")]
        [UIHint("MultiSelect")]
        public IList<string> SearchCustomerGroupIds { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.CustomerTags")]
        public IList<SelectListItem> AvailableCustomerTags { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.CustomerTags")]
        [UIHint("MultiSelect")]
        public IList<string> SearchCustomerTagIds { get; set; }


        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchEmail")]
        
        public string SearchEmail { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchUsername")]
        
        public string SearchUsername { get; set; }
        public bool UsernamesEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchFirstName")]
        
        public string SearchFirstName { get; set; }
        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchLastName")]
        
        public string SearchLastName { get; set; }


        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchCompany")]
        
        public string SearchCompany { get; set; }
        public bool CompanyEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchPhone")]
        
        public string SearchPhone { get; set; }
        public bool PhoneEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.List.SearchZipCode")]
        
        public string SearchZipPostalCode { get; set; }
        public bool ZipPostalCodeEnabled { get; set; }
    }
}