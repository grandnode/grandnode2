using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Models.ShoppingCart;

public class EstimateShippingModel : BaseModel
{
    public bool Enabled { get; set; }

    [GrandResourceDisplayName("ShoppingCart.EstimateShipping.Country")]
    public string CountryId { get; set; }

    [GrandResourceDisplayName("ShoppingCart.EstimateShipping.StateProvince")]
    public string StateProvinceId { get; set; }

    [GrandResourceDisplayName("ShoppingCart.EstimateShipping.ZipPostalCode")]
    public string ZipPostalCode { get; set; }

    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();
}

public class EstimateShippingResultModel : BaseModel
{
    public IList<ShippingOptionModel> ShippingOptions { get; set; } = new List<ShippingOptionModel>();

    public IList<string> Warnings { get; set; } = new List<string>();

    #region Nested Classes

    public class ShippingOptionModel : BaseModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Price { get; set; }
    }

    #endregion
}