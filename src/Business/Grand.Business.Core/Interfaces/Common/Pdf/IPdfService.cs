using Grand.Domain.Orders;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Common.Pdf
{
    /// <summary>
    /// Customer service interface
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Print an order to PDF
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        /// <returns>A path of generated file</returns>
        Task<string> PrintOrderToPdf(Order order, string languageId, string vendorId = "");

        /// <summary>
        /// Save an order PDF
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        /// <returns>A download ident</returns>
        Task<string> SaveOrderToBinary(Order order, string languageId, string vendorId = "");

        /// <summary>
        /// Print orders to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        Task PrintOrdersToPdf(Stream stream, IList<Order> orders, string languageId = "", string vendorId = "");

        /// <summary>
        /// Print packaging slips to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        Task PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, string languageId = "");

    }
}