using Grand.Infrastructure.ModelBinding;

namespace Tax.FixedRate.Models;

public class FixedTaxRateModel
{
    public string TaxCategoryId { get; set; }

    /// <summary>
    ///     Store the rate belongs to. Empty means the shared (global) rate, which is read-only
    ///     in the store panel; the current store id means a store specific, editable rate.
    /// </summary>
    public string StoreId { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.FixedRate.Fields.TaxCategoryName")]
    public string TaxCategoryName { get; set; }

    [GrandResourceDisplayName("Plugins.Tax.FixedRate.Fields.Rate")]
    public double Rate { get; set; }
}