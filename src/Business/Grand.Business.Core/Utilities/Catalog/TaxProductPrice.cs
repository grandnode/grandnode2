namespace Grand.Business.Core.Utilities.Catalog;

public class TaxProductPrice
{
    public double UnitPriceWithoutDiscInclTax { get; set; }
    public double UnitPriceWithoutDiscExclTax { get; set; }
    public double UnitPriceInclTax { get; set; }
    public double UnitPriceExclTax { get; set; }
    public int Quantity { get; set; }
    public double SubTotalInclTax { get; set; }
    public double SubTotalExclTax { get; set; }
    public double DiscountAmountInclTax { get; set; }
    public double DiscountAmountExclTax { get; set; }
    public double TaxRate { get; set; }
}