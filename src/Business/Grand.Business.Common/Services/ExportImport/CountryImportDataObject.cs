using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Directory;

namespace Grand.Business.Common.Services.ExportImport
{
    public class CountryImportDataObject : IImportDataObject<CountryStates>
    {
        private readonly ICountryService _countryService;

        public CountryImportDataObject(ICountryService countryService)
        {
            _countryService = countryService;
        }

        public async Task Execute(IEnumerable<CountryStates> data)
        {
            foreach (var item in data)
            {
                await Import(item);
            }
        }

        private async Task Import(CountryStates countryStatesDto)
        {
            var country = await _countryService.GetCountryByTwoLetterIsoCode(countryStatesDto.Country);
            if (country == null)
                return;

            var state = country.StateProvinces.FirstOrDefault(x => x.Abbreviation.Equals(countryStatesDto.Abbreviation, StringComparison.OrdinalIgnoreCase));
            if (state != null)
            {
                state.Name = countryStatesDto.StateProvinceName;
                state.Abbreviation = countryStatesDto.Abbreviation;
                state.Published = countryStatesDto.Published;
                state.DisplayOrder = countryStatesDto.DisplayOrder;
                await _countryService.UpdateStateProvince(state, country.Id);
            }
            else
            {
                state = new StateProvince {
                    Name = countryStatesDto.StateProvinceName,
                    Abbreviation = countryStatesDto.Abbreviation,
                    Published = countryStatesDto.Published,
                    DisplayOrder = countryStatesDto.DisplayOrder
                };
                await _countryService.InsertStateProvince(state, country.Id);
            }
        }


    }
}
