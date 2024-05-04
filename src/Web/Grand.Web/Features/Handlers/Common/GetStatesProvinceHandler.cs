using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using MediatR;

namespace Grand.Web.Features.Handlers.Common;

public class GetStatesProvinceHandler : IRequestHandler<GetStatesProvince, IList<StateProvinceModel>>
{
    private readonly ICountryService _countryService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public GetStatesProvinceHandler(ICountryService countryService, IWorkContext workContext,
        ITranslationService translationService)
    {
        _countryService = countryService;
        _workContext = workContext;
        _translationService = translationService;
    }

    public async Task<IList<StateProvinceModel>> Handle(GetStatesProvince request, CancellationToken cancellationToken)
    {
        var states =
            await _countryService.GetStateProvincesByCountryId(request.CountryId, _workContext.WorkingLanguage.Id);
        var model = (from s in states
            select new StateProvinceModel
                { id = s.Id, name = s.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id) }).ToList();
        if (request.AddSelectStateItem)
            model.Insert(0,
                new StateProvinceModel { id = "", name = _translationService.GetResource("Address.SelectState") });
        return model;
    }
}