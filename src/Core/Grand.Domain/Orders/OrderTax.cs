namespace Grand.Domain.Orders
{
    public class OrderTax : SubBaseEntity
    {
        public decimal Percent { get; set; }

        public decimal Amount { get; set; }
    }
}
