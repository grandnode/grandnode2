using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class CountryController : BasePublicController
{
    #region Constructors

    public CountryController(IMediator mediator, IRequestTranslationService translationService)
    {
        _mediator = mediator;
        _translationService = translationService;
    }

    #endregion

    #region States / provinces

    //available even when navigation is not allowed
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> GetStatesByCountryId(string countryId, bool addSelectStateItem)
    {
        //this action method gets called via an ajax request
        if (string.IsNullOrEmpty(countryId))
            return Json(new List<StateProvinceModel>
                { new() { id = "", name = _translationService.GetResource("Address.SelectState") } });
        var model = await _mediator.Send(new GetStatesProvince
            { CountryId = countryId, AddSelectStateItem = addSelectStateItem });
        return Json(model);
    }

    #endregion

    #region Fields

    private readonly IMediator _mediator;
    private readonly IRequestTranslationService _translationService;

    #endregion
}