using Grand.Domain.Data;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Install
{
    public partial class InstallModel : BaseModel
    {
        public InstallModel()
        {
            AvailableLanguages = new List<SelectListItem>();
            AvailableCollation = new List<SelectListItem>();
            AvailableProviders = new List<SelectListItem>();
        }
        [DataType(DataType.EmailAddress)]
        public string AdminEmail { get; set; }
        [DataType(DataType.Password)]
        public string AdminPassword { get; set; }
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        public string DatabaseConnectionString { get; set; }
        public DbProvider DataProvider { get; set; }
        public bool ConnectionInfo { get; set; }
        public string MongoDBServerName { get; set; }
        public string MongoDBDatabaseName { get; set; }
        public string MongoDBUsername { get; set; }
        [DataType(DataType.Password)]
        public string MongoDBPassword { get; set; }
        public bool DisableSampleDataOption { get; set; }
        public bool InstallSampleData { get; set; }
        public bool Installed { get; set; }
        public string Collation { get; set; }
        public List<SelectListItem> AvailableLanguages { get; set; }
        public List<SelectListItem> AvailableCollation { get; set; }
        public List<SelectListItem> AvailableProviders { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string CompanyEmail { get; set; }
    }
}