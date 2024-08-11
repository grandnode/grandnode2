using AutoMapper;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Directory;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Services;

public class CountryViewModelService : ICountryViewModelService
{
    private readonly ICountryService _countryService;
    private readonly IMapper _mapper;

    public CountryViewModelService(ICountryService countryService,
        IMapper mapper)
    {
        _countryService = countryService;
        _mapper = mapper;
    }

    public virtual CountryModel PrepareCountryModel()
    {
        var model = new CountryModel {
            //default values
            Published = true,
            AllowsBilling = true,
            AllowsShipping = true
        };
        return model;
    }

    public virtual async Task<Country> InsertCountryModel(CountryModel model)
    {
        var country = _mapper.Map<Country>(model);
        await _countryService.InsertCountry(country);
        return country;
    }

    public virtual async Task<Country> UpdateCountryModel(Country country, CountryModel model)
    {
        country = _mapper.Map(model, country);
        await _countryService.UpdateCountry(country);
        return country;
    }

    public virtual StateProvinceModel PrepareStateProvinceModel(string countryId)
    {
        var model = new StateProvinceModel {
            CountryId = countryId,
            //default value
            Published = true
        };
        return model;
    }

    public virtual async Task<StateProvince> InsertStateProvinceModel(StateProvinceModel model)
    {
        var sp = _mapper.Map<StateProvince>(model);
        await _countryService.InsertStateProvince(sp, model.CountryId);
        return sp;
    }

    public virtual async Task<StateProvince> UpdateStateProvinceModel(StateProvince sp, StateProvinceModel model)
    {
        sp = _mapper.Map(model, sp);
        await _countryService.UpdateStateProvince(sp, model.CountryId);
        return sp;
    }
}