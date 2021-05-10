using Grand.Domain;
using Grand.Domain.Vendors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Interfaces
{
    /// <summary>
    /// Vendor service interface
    /// </summary>
    public partial interface IVendorService
    {
        /// <summary>
        /// Gets a vendor by vendor identifier
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>Vendor</returns>
        Task<Vendor> GetVendorById(string vendorId);

        /// <summary>
        /// Gets all vendors
        /// </summary>
        /// <param name="name">Vendor name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Vendors</returns>
        Task<IPagedList<Vendor>> GetAllVendors(string name = "", 
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

        /// <summary>
        /// Inserts a vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        Task InsertVendor(Vendor vendor);

        /// <summary>
        /// Updates the vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        Task UpdateVendor(Vendor vendor);


        /// <summary>
        /// Delete a vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        Task DeleteVendor(Vendor vendor);

        /// <summary>
        /// Gets a vendor note note
        /// </summary>
        /// <param name="vendorId">The vendor identifier</param>
        /// <param name="vendorNoteId">The vendor note identifier</param>
        /// <returns>Vendor note</returns>
        Task<VendorNote> GetVendorNoteById(string vendorId,string vendorNoteId);

        /// <summary>
        /// Insert a vendor note
        /// </summary>
        /// <param name="vendorNote">The vendor note</param>
        /// <param name="vendorId">Vendor ident</param>
        Task InsertVendorNote(VendorNote vendorNote, string vendorId);
        /// <summary>
        /// Deletes a vendor note
        /// </summary>
        /// <param name="vendorNote">The vendor note</param>
        /// <param name="vendorId">Vendor ident</param>
        Task DeleteVendorNote(VendorNote vendorNote, string vendorId);

        /// <summary>
        /// Gets a vendor mapping 
        /// </summary>
        /// <param name="discountId">Discount id mapping identifier</param>
        /// <returns>vendor mapping</returns>
        Task<IList<Vendor>> GetAllVendorsByDiscount(string discountId);

        #region Vendor reviews

        /// <summary>
        /// Update Vendor review totals
        /// </summary>
        /// <param name="Vendor">Vendor</param>
        Task UpdateVendorReviewTotals(Vendor Vendor);

        /// <summary>
        /// Update Vendor review 
        /// </summary>
        /// <param name="Vendorreview">Vendorreview</param>
        Task UpdateVendorReview(VendorReview Vendorreview);

        /// <summary>
        /// Insert Vendor review 
        /// </summary>
        /// <param name="Vendorreview">Vendorreview</param>
        Task InsertVendorReview(VendorReview Vendorreview);

        /// <summary>
        /// Gets all vendor reviews
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <param name="approved">A value indicating whether to content is approved; null to load all records</param> 
        /// <param name="fromUtc">Item creation from; null to load all records</param>
        /// <param name="toUtc">Item item creation to; null to load all records</param>
        /// <param name="message">Search title or review text; null to load all records</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <returns>Reviews</returns>
        Task<IPagedList<VendorReview>> GetAllVendorReviews(string customerId, bool? approved,
            DateTime? fromUtc = null, DateTime? toUtc = null,
            string message = null, string vendorId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets vendor review
        /// </summary>
        /// <param name="vendorReviewId">Vendor review identifier</param>
        /// <returns>Vendor review</returns>
        Task<VendorReview> GetVendorReviewById(string vendorReviewId);

        /// <summary>
        /// Deletes a vendor review
        /// </summary>
        /// <param name="vendorReview">Vendor review</param>
        Task DeleteVendorReview(VendorReview vendorReview);

        /// <summary>
        /// Search vendors
        /// </summary>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <param name="keywords">Keywords</param>
        /// <returns>Vendors</returns>
        Task<IList<Vendor>> SearchVendors(string vendorId = "", string keywords = null);

        #endregion
    }
}