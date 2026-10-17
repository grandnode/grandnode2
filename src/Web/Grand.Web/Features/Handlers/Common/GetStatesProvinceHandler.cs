using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using Grand.Mediator;
using Grand.Web.Common.Localization;

namespace Grand.Web.Features.Handlers.Common;

public class GetStatesProvinceHandler : IRequestHandler<GetStatesProvince, IList<StateProvinceModel>>
{
    private readonly ICountryService _countryService;
    private readonly IRequestTranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public GetStatesProvinceHandler(ICountryService countryService, IContextAccessor contextAccessor,
        IRequestTranslationService translationService)
    {
        _countryService = countryService;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
    }

    public async Task<IList<StateProvinceModel>> Handle(GetStatesProvince request, CancellationToken cancellationToken)
    {
        var states =
            await _countryService.GetStateProvincesByCountryId(request.CountryId, _contextAccessor.WorkContext.WorkingLanguage.Id);
        var model = (from s in states
            select new StateProvinceModel
                { id = s.Id, name = s.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id) }).ToList();
        if (request.AddSelectStateItem)
            model.Insert(0,
                new StateProvinceModel { id = "", name = _translationService.GetResource("Address.SelectState") });
        return model;
    }
}