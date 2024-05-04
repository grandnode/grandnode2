using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tax.CountryStateZip.Models;

public class TaxRateListModel : BaseModel
{
    [GrandResourceDisplayName("Plugins.Tax.CountryStateZip.Fields.Store")]
    public string AddStoreId { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.CountryStateZip.Fields.Country")]
    public string AddCountryId { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.CountryStateZip.Fields.StateProvince")]
    public string AddStateProvinceId { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.CountryStateZip.Fields.Zip")]
    public string AddZip { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.CountryStateZip.Fields.TaxCategory")]
    public string AddTaxCategoryId { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.CountryStateZip.Fields.Percentage")]
    public double AddPercentage { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableTaxCategories { get; set; } = new List<SelectListItem>();

    public IList<TaxRateModel> TaxRates { get; set; } = new List<TaxRateModel>();
}