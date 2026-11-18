namespace Grand.Domain.Payments;

/// <summary>
///     Kind of money operation recorded on a payment transaction
/// </summary>
public enum PaymentTransactionEntryType
{
    Payment = 10,
    Authorization = 20,
    Capture = 30,
    Refund = 40,
    Void = 50
}
