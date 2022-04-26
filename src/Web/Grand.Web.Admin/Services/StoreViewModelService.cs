using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Stores;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services
{
    public partial class StoreViewModelService : IStoreViewModelService
    {
        private readonly ILanguageService _languageService;
        private readonly IWarehouseService _warehouseService;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;

        public StoreViewModelService(ILanguageService languageService, IWarehouseService warehouseService, IStoreService storeService, ISettingService settingService,
            ICountryService countryService, ICurrencyService currencyService)
        {
            _languageService = languageService;
            _warehouseService = warehouseService;
            _storeService = storeService;
            _settingService = settingService;
            _countryService = countryService;
            _currencyService = currencyService;
        }

        public virtual async Task PrepareLanguagesModel(StoreModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableLanguages.Add(new SelectListItem {
                Text = "---",
                Value = ""
            });
            var languages = await _languageService.GetAllLanguages(true);
            foreach (var language in languages)
            {
                model.AvailableLanguages.Add(new SelectListItem {
                    Text = language.Name,
                    Value = language.Id
                });
            }
        }

        public virtual async Task PrepareWarehouseModel(StoreModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //warehouse
            model.AvailableWarehouses.Add(new SelectListItem {
                Text = "---",
                Value = ""
            });
            var warehouses = await _warehouseService.GetAllWarehouses();
            foreach (var warehouse in warehouses)
            {
                model.AvailableWarehouses.Add(new SelectListItem {
                    Text = warehouse.Name,
                    Value = warehouse.Id
                });
            }
        }
        public virtual async Task PrepareCountryModel(StoreModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //countries
            model.AvailableCountries.Add(new SelectListItem {
                Text = "---",
                Value = ""
            });

            var countries = await _countryService.GetAllCountries();
            foreach (var country in countries)
            {
                model.AvailableCountries.Add(new SelectListItem {
                    Text = country.Name,
                    Value = country.Id
                });
            }
        }
        public virtual async Task PrepareCurrencyModel(StoreModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //countries
            model.AvailableCurrencies.Add(new SelectListItem {
                Text = "---",
                Value = ""
            });

            var currencies = await _currencyService.GetAllCurrencies();
            foreach (var currency in currencies)
            {
                model.AvailableCurrencies.Add(new SelectListItem {
                    Text = currency.Name,
                    Value = currency.Id
                });
            }
        }
        public virtual StoreModel PrepareStoreModel()
        {
            var model = new StoreModel();
            return model;
        }
        public virtual async Task<Store> InsertStoreModel(StoreModel model)
        {
            var store = model.ToEntity();
            //ensure we have "/" at the end
            if (!store.Url.Trim().EndsWith("/"))
                store.Url = store.Url.Trim() + "/";

            if (!string.IsNullOrEmpty(store.SecureUrl) && !store.SecureUrl.Trim().EndsWith("/"))
                store.SecureUrl = store.SecureUrl.Trim() + "/";

            var storeUri = new Uri(store.Url);

            store.Domains.Add(new DomainHost() {
                HostName = storeUri.Host,
                Url = store.SslEnabled ? store.SecureUrl : store.Url,
                Primary = true
            });

            await _storeService.InsertStore(store);
            return store;
        }
        public virtual async Task<Store> UpdateStoreModel(Store store, StoreModel model)
        {
            store = model.ToEntity(store);

            //ensure we have "/" at the end
            if (!store.Url.Trim().EndsWith("/"))
                store.Url = store.Url.Trim() + "/";
            if (!string.IsNullOrEmpty(store.SecureUrl) && !store.SecureUrl.Trim().EndsWith("/"))
                store.SecureUrl = store.SecureUrl.Trim() + "/";

            var domain = store.Domains.FirstOrDefault(x => x.Primary);
            if (domain == null)
            {
                var storeUri = new Uri(store.Url);
                store.Domains.Add(new DomainHost() {
                    HostName = storeUri.Host,
                    Url = store.SslEnabled ? store.SecureUrl : store.Url,
                    Primary = true
                });
            }
            else
            {
                var storeUri = new Uri(store.Url);
                domain.HostName = storeUri.Host;
                domain.Url = store.SslEnabled ? store.SecureUrl : store.Url;
            }

            await _storeService.UpdateStore(store);
            return store;
        }
        public virtual async Task DeleteStore(Store store)
        {
            await _storeService.DeleteStore(store);

            //when we delete a store we should also ensure that all "per store" settings will also be deleted
            var settingsToDelete = _settingService
                .GetAllSettings()
                .Where(s => s.StoreId == store.Id)
                .ToList();
            foreach (var setting in settingsToDelete)
                await _settingService.DeleteSetting(setting);
            //when we had two stores and now have only one store, we also should delete all "per store" settings
            var allStores = await _storeService.GetAllStores();
            if (allStores.Count == 1)
            {
                settingsToDelete = _settingService
                    .GetAllSettings()
                    .Where(s => s.StoreId == allStores[0].Id)
                    .ToList();
                foreach (var setting in settingsToDelete)
                    await _settingService.DeleteSetting(setting);
            }
        }

        #region DomainHost

        public virtual async Task InsertDomainHostModel(Store store, DomainHostModel model)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!model.Url.EndsWith("/"))
                model.Url += "/";

            var storeUri = new Uri(model.Url);

            store.Domains.Add(new DomainHost() {
                HostName = storeUri.Host,
                Url = model.Url
            });
            await _storeService.UpdateStore(store);

        }

        public virtual async Task UpdateDomainHostModel(Store store, DomainHostModel model)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            
            if (!model.Url.EndsWith("/"))
                model.Url += "/";

            var domain = store.Domains.FirstOrDefault(x => x.Id == model.Id);
            if (domain != null)
            {
                var storeUri = new Uri(model.Url);
                domain.HostName = storeUri.Host;
                domain.Url = model.Url;
            }

            await _storeService.UpdateStore(store);
        }

        public virtual async Task DeleteDomainHostModel(Store store, string id)
        {
            var domain = store.Domains.FirstOrDefault(x => x.Id == id);
            if (domain != null)
            {
                store.Domains.Remove(domain);
                await _storeService.UpdateStore(store);
            }

        }

        #endregion

    }
}
