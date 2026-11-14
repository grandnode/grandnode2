using Grand.Domain.Orders;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Common.Pdf;

/// <summary>
///     Customer service interface
/// </summary>
public interface IPdfService
{
    /// <summary>
    ///     Save an order PDF
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
    /// <param name="vendorId">
    ///     Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't
    ///     be printed
    /// </param>
    /// <returns>A download ident</returns>
    Task<string> SaveOrderToBinary(Order order, string languageId, string vendorId = "");

    /// <summary>
    ///     Print orders to PDF
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="orders">Orders</param>
    /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
    /// <param name="vendorId">
    ///     Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't
    ///     be printed
    /// </param>
    /// <param name="cancellationToken">Stops the work before the PDF is generated</param>
    Task PrintOrdersToPdf(Stream stream, IList<Order> orders, string languageId = "", string vendorId = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Print packaging slips to PDF
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="shipments">Shipments</param>
    /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
    /// <param name="cancellationToken">Stops the work before the PDF is generated</param>
    Task PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, string languageId = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Print the document a customer sends with the items of a merchandise return: what is returned, why, and
    ///     where it goes
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="order">The order of the return</param>
    /// <param name="languageId">Language identifier; empty to use the language the order was placed in</param>
    /// <param name="cancellationToken">Stops the work before the PDF is generated</param>
    Task PrintMerchandiseReturnToPdf(Stream stream, MerchandiseReturn merchandiseReturn, Order order,
        string languageId = "", CancellationToken cancellationToken = default);
}