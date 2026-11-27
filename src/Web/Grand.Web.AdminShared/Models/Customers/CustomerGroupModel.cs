using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Customers;

public class CustomerGroupModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.Name")]

    public string? Name { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.FreeShipping")]

    public bool FreeShipping { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.TaxExempt")]
    public bool TaxExempt { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.Active")]
    public bool Active { get; set; }

    /// <summary>
    /// Shown, never edited: the system groups are seeded by the installer. Never bound, so a form
    /// cannot take the protection off a system group or give it to a new one.
    /// </summary>
    [BindNever]
    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.IsSystem")]
    public bool IsSystem { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.SystemName")]
    public string? SystemName { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.EnablePasswordLifetime")]
    public bool EnablePasswordLifetime { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.MinOrderAmount")]
    [UIHint("DoubleNullable")]
    public double? MinOrderAmount { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.MaxOrderAmount")]
    [UIHint("DoubleNullable")]
    public double? MaxOrderAmount { get; set; }
}