namespace Grand.Domain.Orders
{
    public class OrderTax : SubBaseEntity
    {
        public double Percent { get; set; }

        public double Amount { get; set; }
    }
}
