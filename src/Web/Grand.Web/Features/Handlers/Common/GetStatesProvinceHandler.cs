using Grand.Infrastructure;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Common;
using Grand.Web.Features.Models.Common;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Common.Extensions;

namespace Grand.Web.Features.Handlers.Common
{
    public class GetStatesProvinceHandler : IRequestHandler<GetStatesProvince, IList<StateProvinceModel>>
    {
        private readonly ICountryService _countryService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;

        public GetStatesProvinceHandler(ICountryService countryService, IWorkContext workContext, ITranslationService translationService)
        {
            _countryService = countryService;
            _workContext = workContext;
            _translationService = translationService;
        }

        public async Task<IList<StateProvinceModel>> Handle(GetStatesProvince request, CancellationToken cancellationToken)
        {
            var states = await _countryService.GetStateProvincesByCountryId(request.CountryId, _workContext.WorkingLanguage.Id);
            var model = (from s in states select new StateProvinceModel { id = s.Id, name = s.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id) }).ToList();
            if (request.AddSelectStateItem)
            {
                model.Insert(0, new StateProvinceModel { id = "", name = _translationService.GetResource("Address.SelectState") });
            }
            return model;
        }
    }
}
