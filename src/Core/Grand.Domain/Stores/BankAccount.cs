namespace Grand.Domain.Stores
{
    public class BankAccount : SubBaseEntity
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string SwiftCode { get; set; }
        public string AccountNumber { get; set; }

    }
}
