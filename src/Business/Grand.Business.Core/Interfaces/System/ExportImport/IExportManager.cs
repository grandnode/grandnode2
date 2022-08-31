﻿using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Messages;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.System.ExportImport
{
    /// <summary>
    /// Export manager interface
    /// </summary>
    public partial interface IExportManager
    {
        /// <summary>
        /// Export collections to XLSX
        /// </summary>
        /// <param name="collections">Brands</param>
        byte[] ExportBrandsToXlsx(IEnumerable<Brand> brands);

        /// <summary>
        /// Export collections to XLSX
        /// </summary>
        /// <param name="collections">Collections</param>
        byte[] ExportCollectionsToXlsx(IEnumerable<Collection> collections);
        

        /// <summary>
        /// Export category to XLSX
        /// </summary>
        /// <param name="categories">Categories</param>
        byte[] ExportCategoriesToXlsx(IEnumerable<Category> categories);
        

        /// <summary>
        /// Export products to XLSX
        /// </summary>
        /// <param name="products">Products</param>
        byte[] ExportProductsToXlsx(IEnumerable<Product> products);

        /// <summary>
        /// Export orders to XLSX
        /// </summary>
        /// <param name="orders">Orders</param>
        byte[] ExportOrdersToXlsx(IList<Order> orders);

        /// <summary>
        /// Export customer list to XLSX
        /// </summary>
        /// <param name="customers">Customers</param>
        byte[] ExportCustomersToXlsx(IList<Customer> customers);

        /// <summary>
        /// Export customer - personal info to XLSX
        /// </summary>
        /// <param name="customer">Customer</param>
        Task<byte[]> ExportCustomerToXlsx(Customer customer, string storeId);

        /// <summary>
        /// Export states to XLSX
        /// </summary>
        /// <param name="countries">Countries</param>
        byte[] ExportStatesToXlsx(IList<Country> countries);

        /// <summary>
        /// Export newsletter subscribers to TXT
        /// </summary>
        /// <param name="subscriptions">Subscriptions</param>
        /// <returns>Result in TXT (string) format</returns>
        string ExportNewsletterSubscribersToTxt(IList<NewsLetterSubscription> subscriptions);

        /// <summary>
        /// Export newsletter subscribers to TXT
        /// </summary>
        /// <param name="subscriptions">Subscriptions</param>
        /// <returns>Result in TXT (string) format</returns>
        string ExportNewsletterSubscribersToTxt(IList<string> subscriptions);
       
    }
}
