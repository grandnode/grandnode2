namespace Grand.Business.Catalog.Services.Tax
{
    public class TaxProductPrice
    {
        public double UnitPriceWihoutDiscInclTax { get; set; }
        public double UnitPriceWihoutDiscExclTax { get; set; }
        public double UnitPriceInclTax { get; set; }
        public double UnitPriceExclTax { get; set; }
        public int Quantity { get; set; }
        public double SubTotalInclTax { get; set; }
        public double SubTotalExclTax { get; set; }
        public double discountAmountInclTax { get; set; }
        public double discountAmountExclTax { get; set; }
        public double taxRate { get; set; }
    }
}
