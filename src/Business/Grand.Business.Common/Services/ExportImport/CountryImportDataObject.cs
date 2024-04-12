using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Directory;

namespace Grand.Business.Common.Services.ExportImport;

public class CountryImportDataObject : IImportDataObject<CountryStatesDto>
{
    private readonly ICountryService _countryService;

    public CountryImportDataObject(ICountryService countryService)
    {
        _countryService = countryService;
    }

    public async Task Execute(IEnumerable<CountryStatesDto> data)
    {
        foreach (var item in data) await Import(item);
    }

    private async Task Import(CountryStatesDto countryStatesDtoDto)
    {
        var country = await _countryService.GetCountryByTwoLetterIsoCode(countryStatesDtoDto.Country);
        if (country == null)
            return;

        var state = country.StateProvinces.FirstOrDefault(x =>
            x.Abbreviation.Equals(countryStatesDtoDto.Abbreviation, StringComparison.OrdinalIgnoreCase));
        if (state != null)
        {
            state.Name = countryStatesDtoDto.StateProvinceName;
            state.Abbreviation = countryStatesDtoDto.Abbreviation;
            state.Published = countryStatesDtoDto.Published;
            state.DisplayOrder = countryStatesDtoDto.DisplayOrder;
            await _countryService.UpdateStateProvince(state, country.Id);
        }
        else
        {
            state = new StateProvince {
                Name = countryStatesDtoDto.StateProvinceName,
                Abbreviation = countryStatesDtoDto.Abbreviation,
                Published = countryStatesDtoDto.Published,
                DisplayOrder = countryStatesDtoDto.DisplayOrder
            };
            await _countryService.InsertStateProvince(state, country.Id);
        }
    }
}