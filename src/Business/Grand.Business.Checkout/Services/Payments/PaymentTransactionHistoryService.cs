using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using System.Globalization;
using System.Text;

namespace Grand.Business.Checkout.Services.Payments;

public class PaymentTransactionHistoryService(
    IRepository<PaymentTransactionEntry> entryRepository,
    IOrderService orderService,
    ICurrencyService currencyService) : IPaymentTransactionHistoryService
{
    public async Task<PaymentTransactionEntry> Record(PaymentTransaction paymentTransaction, Order order,
        PaymentTransactionEntryType entryType, double amount, string recordedBy, string reference = null,
        string note = null)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        //in the currency's precision, so a difference of two amounts carries no floating-point remainder
        var currency = await currencyService.GetCurrencyByCode(paymentTransaction.CurrencyCode);
        var decimals = currency?.NumberDecimal ?? 2;
        var entry = new PaymentTransactionEntry {
            PaymentTransactionId = paymentTransaction.Id,
            EntryType = entryType,
            Amount = Math.Round(amount, decimals, currency?.MidpointRoundId ?? MidpointRounding.ToEven),
            RecordedBy = Clean(recordedBy),
            Reference = Clean(reference),
            Note = Clean(note)
        };
        await entryRepository.InsertAsync(entry);

        if (order != null)
            await orderService.InsertOrderNote(new OrderNote {
                Note = ToOrderNote(entry, decimals, paymentTransaction.CurrencyCode),
                DisplayToCustomer = false,
                OrderId = order.Id
            });

        return entry;
    }

    public async Task<IList<PaymentTransactionEntry>> GetEntries(string paymentTransactionId)
    {
        return await entryRepository.ToListAsync(entryRepository.Table
            .Where(x => x.PaymentTransactionId == paymentTransactionId)
            .OrderBy(x => x.CreatedOnUtc));
    }

    /// <summary>
    ///     The order note that records the same operation, e.g. "Payment of 1,250.00 EUR recorded by staff@shop.com.
    ///     Reference: TR-881"
    /// </summary>
    private static string ToOrderNote(PaymentTransactionEntry entry, int decimals, string currencyCode)
    {
        var note = new StringBuilder();
        note.Append(CultureInfo.InvariantCulture,
            $"{entry.EntryType} of {entry.Amount.ToString("N" + decimals, CultureInfo.InvariantCulture)} {currencyCode} recorded");
        if (entry.RecordedBy != null)
            note.Append(CultureInfo.InvariantCulture, $" by {entry.RecordedBy}");
        if (entry.Reference != null)
            note.Append(CultureInfo.InvariantCulture, $". Reference: {entry.Reference}");
        if (entry.Note != null)
            note.Append(CultureInfo.InvariantCulture, $". Note: {entry.Note}");
        return note.ToString();
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
