namespace Grand.Web.AdminShared.Models.Orders;

public class PaymentTransactionEntryModel
{
    public string Operation { get; set; }
    public double Amount { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
    public string Reference { get; set; }
    public string Note { get; set; }
}
