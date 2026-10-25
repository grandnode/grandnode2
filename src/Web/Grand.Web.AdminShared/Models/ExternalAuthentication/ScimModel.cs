using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.ExternalAuthentication;

/// <summary>The SCIM provisioning screen: the state of the endpoint (never the token) and the accounts in a role conflict</summary>
public class ScimModel
{
    /// <summary>FeatureManagement:Grand.Module.Scim</summary>
    public bool ModuleEnabled { get; set; }

    /// <summary>Only whether Scim:Token is long enough, never the token itself</summary>
    public bool TokenUsable { get; set; }

    /// <summary>The OpenID Connect sign-in SCIM depends on is configured</summary>
    public bool SignInConfigured { get; set; }

    public string EndpointUrl { get; set; }
    public int ManagedAccounts { get; set; }
    public IList<ScimConflictModel> Conflicts { get; set; } = [];
}

public class ScimConflictModel
{
    public string CustomerId { get; set; }
    public string Email { get; set; }
}

/// <summary>A row of the grid of groups received from the identity provider</summary>
public class ScimGroupRowModel
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public int MemberCount { get; set; }
    public string CustomerGroup { get; set; }
    public string Store { get; set; }
    public string Vendor { get; set; }
}

/// <summary>The role the members of a SCIM group get</summary>
public class ScimGroupMappingModel : BaseModel
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public int MemberCount { get; set; }

    [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Scim.Fields.CustomerGroup")]
    public string CustomerGroupId { get; set; }

    [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Scim.Fields.StaffStore")]
    public string StaffStoreId { get; set; }

    [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Scim.Fields.Vendor")]
    public string VendorId { get; set; }

    public IList<SelectListItem> AvailableCustomerGroups { get; set; } = [];
    public IList<SelectListItem> AvailableStores { get; set; } = [];
    public IList<SelectListItem> AvailableVendors { get; set; } = [];
}
