using Grand.Domain.Orders;
using System.Globalization;
using System.Security.Cryptography;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Identifies the content of a shopping cart, so that an approval holds for exactly the cart it was given for
/// </summary>
public static class OrderApprovalFingerprint
{
    /// <summary>
    ///     Computes the SHA-256 (hex) of the shopping cart items: product, quantity, attributes, rental dates,
    ///     entered price and warehouse. Wishlist items are ignored; the order of items and attributes does not matter.
    /// </summary>
    /// <param name="cart">Shopping cart items</param>
    public static string Compute(IEnumerable<ShoppingCartItem> cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        // every line starts with the product id, so sorting the lines sorts by product first and keeps
        // items of one product with different attributes in a stable order
        var lines = cart
            .Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            .Select(Line)
            .OrderBy(x => x, StringComparer.Ordinal);

        var canonical = string.Concat(lines);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Line(ShoppingCartItem item)
    {
        var attributes = (item.Attributes ?? [])
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .Select(x => Field(x.Key) + Field(x.Value));

        return Field(item.ProductId)
               + Field(item.Quantity.ToString(CultureInfo.InvariantCulture))
               + Field(string.Concat(attributes))
               + Field(Date(item.RentalStartDateUtc))
               + Field(Date(item.RentalEndDateUtc))
               + Field(item.EnteredPrice?.ToString("R", CultureInfo.InvariantCulture))
               + Field(item.WarehouseId)
               + ";";
    }

    // length prefix: no value can run into the next one and produce the same text from other data
    private static string Field(string value)
    {
        return value is null ? "-;" : $"{value.Length}:{value};";
    }

    private static string Date(DateTime? value)
    {
        if (!value.HasValue) return null;
        var utc = value.Value.Kind == DateTimeKind.Local ? value.Value.ToUniversalTime() : value.Value;
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
    }
}
