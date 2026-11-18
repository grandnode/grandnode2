using System.Globalization;
using System.Text;
using Grand.Domain.Payments;

namespace Grand.Business.Checkout.Extensions;

public static class PaymentTransactionHistory
{
    /// <summary>
    ///     Appends a money operation to the transaction's history
    /// </summary>
    public static PaymentTransactionEntry AddEntry(this PaymentTransaction transaction,
        PaymentTransactionEntryType type, double amount, string createdBy, string reference = null, string note = null)
    {
        var entry = new PaymentTransactionEntry {
            EntryType = type,
            Amount = amount,
            CreatedOnUtc = DateTime.UtcNow,
            CreatedBy = Clean(createdBy),
            Reference = Clean(reference),
            Note = Clean(note)
        };
        transaction.Entries.Add(entry);
        return entry;
    }

    /// <summary>
    ///     The order note that records the same operation, e.g. "Payment of 1,250.00 EUR recorded by staff@shop.com.
    ///     Reference: TR-881"
    /// </summary>
    public static string ToOrderNote(this PaymentTransactionEntry entry, string currencyCode)
    {
        var note = new StringBuilder();
        note.Append(CultureInfo.InvariantCulture, $"{entry.EntryType} of {entry.Amount:N2} {currencyCode} recorded");
        if (entry.CreatedBy != null)
            note.Append(CultureInfo.InvariantCulture, $" by {entry.CreatedBy}");
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
