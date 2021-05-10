using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Directory;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Directory;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CountryViewModelService : ICountryViewModelService
    {
        private readonly ICountryService _countryService;

        public CountryViewModelService(ICountryService countryService)
        {
            _countryService = countryService;
        }

        public virtual CountryModel PrepareCountryModel()
        {
            var model = new CountryModel();
            //default values
            model.Published = true;
            model.AllowsBilling = true;
            model.AllowsShipping = true;
            return model;
        }

        public virtual async Task<Country> InsertCountryModel(CountryModel model)
        {
            var country = model.ToEntity();
            await _countryService.InsertCountry(country);
            return country;
        }

        public virtual async Task<Country> UpdateCountryModel(Country country, CountryModel model)
        {
            country = model.ToEntity(country);
            await _countryService.UpdateCountry(country);
            return country;
        }

        public virtual StateProvinceModel PrepareStateProvinceModel(string countryId)
        {
            var model = new StateProvinceModel();
            model.CountryId = countryId;
            //default value
            model.Published = true;
            return model;
        }

        public virtual async Task<StateProvince> InsertStateProvinceModel(StateProvinceModel model)
        {
            var sp = model.ToEntity();
            await _countryService.InsertStateProvince(sp, model.CountryId);
            return sp;
        }
        public virtual async Task<StateProvince> UpdateStateProvinceModel(StateProvince sp, StateProvinceModel model)
        {
            sp = model.ToEntity(sp);
            await _countryService.UpdateStateProvince(sp, model.CountryId);
            return sp;
        }
    }
}
