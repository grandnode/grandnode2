using Grand.Web.Common.Filters;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Common;
using Grand.Web.Features.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class CountryController : BasePublicController
    {
        #region Fields

        private readonly IMediator _mediator;
        private readonly ITranslationService _translationService;

        #endregion

        #region Constructors

        public CountryController(IMediator mediator, ITranslationService translationService)
        {
            _mediator = mediator;
            _translationService = translationService;
        }

        #endregion

        #region States / provinces

        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> GetStatesByCountryId(string countryId, bool addSelectStateItem)
        {
            //this action method gets called via an ajax request
            if (string.IsNullOrEmpty(countryId))
            {
                return Json(new List<StateProvinceModel>() { new StateProvinceModel { id = "", name = _translationService.GetResource("Address.SelectState") } });
            }
            var model = await _mediator.Send(new GetStatesProvince() { CountryId = countryId, AddSelectStateItem = addSelectStateItem });
            return Json(model);
        }

        #endregion
    }
}
