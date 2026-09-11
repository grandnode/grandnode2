using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

// ARCH-001: HomeController infra actions. Admin/Store/Vendor's HomeController each duplicated
// GetStatesByCountryId and Logout verbatim apart from one parameterizable string each (the
// SelectState resource key, the logout route name) - no entity, no IAdminDataScope, same shape as
// BasePictureController. SetLanguage is Admin+Store only (Vendor never had it), so it lives on
// BaseHomeControllerWithSetLanguage instead of here - putting it on this class would silently give
// Vendor's concrete controller a new, never-existed-before route (MVC discovers every public action
// on the whole inheritance chain), the same two-level split BaseOrderController/
// BaseOrderManagementController already established for Order's Vendor-is-a-subset shape.
//
// DashboardActivity/ChangeStore (Admin-only) and Index/Statistics/AccessDenied (real per-host
// dashboards/views) stay on each concrete host controller, untouched.
public abstract class BaseHomeController(
    ICountryService countryService,
    ITranslationService translationService,
    IGrandAuthenticationService authenticationService)
    : BaseController
{
    /// <summary>
    ///     The resource key for the "select a state" placeholder option. Admin and Store share the
    ///     literal "Admin.Address.SelectState" key (verified identical, not a typo); only Vendor uses
    ///     its own "Vendor.Address.SelectState".
    /// </summary>
    protected abstract string SelectStateResourceKey { get; }

    /// <summary>The named route this host's login page is registered under.</summary>
    protected abstract string LogoutRouteName { get; }

    [AcceptVerbs("Get")]
    public async Task<IActionResult> GetStatesByCountryId(string countryId, bool? addSelectStateItem, bool? addAsterisk)
    {
        // This action method gets called via an ajax request
        if (string.IsNullOrEmpty(countryId))
            return Json(new List<dynamic>
                { new { id = "", name = translationService.GetResource("Address.SelectState") } });

        var country = await countryService.GetCountryById(countryId);
        var states = country != null ? country.StateProvinces.ToList() : [];
        var result = (from s in states
            select new { id = s.Id, name = s.Name }).ToList();
        if (addAsterisk.HasValue && addAsterisk.Value)
        {
            //asterisk
            result.Insert(0, new { id = "", name = "*" });
        }
        else
        {
            if (country == null)
            {
                //country is not selected ("choose country" item)
                if (addSelectStateItem.HasValue && addSelectStateItem.Value)
                    result.Insert(0,
                        new { id = "", name = translationService.GetResource(SelectStateResourceKey) });
            }
            else
            {
                //some country is selected
                if (result.Any() && addSelectStateItem.HasValue && addSelectStateItem.Value)
                    //country has some states
                    result.Insert(0,
                        new { id = "", name = translationService.GetResource(SelectStateResourceKey) });
            }
        }

        return Json(result);
    }

    public async Task<IActionResult> Logout()
    {
        await authenticationService.SignOut();
        return RedirectToRoute(LogoutRouteName);
    }
}
