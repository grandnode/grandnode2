using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Customers.Services;

/// <summary>
///     Vendor service
/// </summary>
public class VendorService : IVendorService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="vendorRepository">Vendor repository</param>
    /// <param name="vendorReviewRepository">Vendor review repository</param>
    /// <param name="mediator">Mediator</param>
    public VendorService(IRepository<Vendor> vendorRepository, IRepository<VendorReview> vendorReviewRepository,
        IMediator mediator)
    {
        _vendorRepository = vendorRepository;
        _vendorReviewRepository = vendorReviewRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<Vendor> _vendorRepository;
    private readonly IRepository<VendorReview> _vendorReviewRepository;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a vendor by vendor identifier
    /// </summary>
    /// <param name="vendorId">Vendor identifier</param>
    /// <returns>Vendor</returns>
    public virtual Task<Vendor> GetVendorById(string vendorId)
    {
        return _vendorRepository.GetByIdAsync(vendorId);
    }

    /// <summary>
    ///     Gets all vendors
    /// </summary>
    /// <param name="name">Vendor name</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <returns>Vendors</returns>
    public virtual async Task<IPagedList<Vendor>> GetAllVendors(string name = "",
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var query = from p in _vendorRepository.Table
            select p;

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(v => v.Name.ToLower().Contains(name.ToLower()));
        if (!showHidden)
            query = query.Where(v => v.Active);
        query = query.Where(v => !v.Deleted);
        query = query.OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name);
        return await PagedList<Vendor>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Inserts a vendor
    /// </summary>
    /// <param name="vendor">Vendor</param>
    public virtual async Task InsertVendor(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        await _vendorRepository.InsertAsync(vendor);

        //event notification
        await _mediator.EntityInserted(vendor);
    }

    /// <summary>
    ///     Updates the vendor
    /// </summary>
    /// <param name="vendor">Vendor</param>
    public virtual async Task UpdateVendor(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        await _vendorRepository.UpdateAsync(vendor);

        //event notification
        await _mediator.EntityUpdated(vendor);
    }

    /// <summary>
    ///     Delete a vendor
    /// </summary>
    /// <param name="vendor">Vendor</param>
    public virtual async Task DeleteVendor(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        vendor.Deleted = true;
        await UpdateVendor(vendor);
    }


    /// <summary>
    ///     Gets a vendor note note
    /// </summary>
    /// <param name="vendorId">Vendor ident</param>
    /// <param name="vendorNoteId">The vendor note identifier</param>
    /// <returns>Vendor note</returns>
    public virtual async Task<VendorNote> GetVendorNoteById(string vendorId, string vendorNoteId)
    {
        if (string.IsNullOrEmpty(vendorNoteId))
            return null;
        var vendor = await _vendorRepository.GetByIdAsync(vendorId);
        return vendor?.VendorNotes.FirstOrDefault(x => x.Id == vendorNoteId);
    }

    /// <summary>
    ///     Insert vendor note
    /// </summary>
    /// <param name="vendorNote"></param>
    /// <param name="vendorId"></param>
    /// <returns></returns>
    public virtual async Task InsertVendorNote(VendorNote vendorNote, string vendorId)
    {
        ArgumentNullException.ThrowIfNull(vendorNote);

        await _vendorRepository.AddToSet(vendorId, x => x.VendorNotes, vendorNote);

        //event notification
        await _mediator.EntityInserted(vendorNote);
    }

    /// <summary>
    ///     Deletes a vendor note
    /// </summary>
    /// <param name="vendorNote">The vendor note</param>
    /// <param name="vendorId">Vendor ident</param>
    public virtual async Task DeleteVendorNote(VendorNote vendorNote, string vendorId)
    {
        ArgumentNullException.ThrowIfNull(vendorNote);

        await _vendorRepository.PullFilter(vendorId, x => x.VendorNotes, x => x.Id, vendorNote.Id);

        //event notification
        await _mediator.EntityDeleted(vendorNote);
    }

    /// <summary>
    ///     Gets a vendor mapping
    /// </summary>
    /// <param name="discountId">Discount id mapping identifier</param>
    /// <returns>vendor mapping</returns>
    public virtual async Task<IList<Vendor>> GetAllVendorsByDiscount(string discountId)
    {
        var query = from c in _vendorRepository.Table
            where c.AppliedDiscounts.Any(x => x == discountId)
            select c;
        return await Task.FromResult(query.ToList());
    }

    #region Vendor reviews

    /// <summary>
    ///     Gets all vendor reviews
    /// </summary>
    /// <param name="customerId">Customer identifier; "" to load all records</param>
    /// <param name="approved">A value indicating whether to content is approved; null to load all records</param>
    /// <param name="fromUtc">Item creation from; null to load all records</param>
    /// <param name="toUtc">Item item creation to; null to load all records</param>
    /// <param name="message">Search title or review text; null to load all records</param>
    /// <param name="vendorId"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns>Reviews</returns>
    public virtual async Task<IPagedList<VendorReview>> GetAllVendorReviews(string customerId, bool? approved,
        DateTime? fromUtc = null, DateTime? toUtc = null,
        string message = null, string vendorId = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from p in _vendorReviewRepository.Table
            select p;

        if (approved.HasValue)
            query = query.Where(c => c.IsApproved == approved.Value);
        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(c => c.CustomerId == customerId);
        if (fromUtc.HasValue)
            query = query.Where(c => fromUtc.Value <= c.CreatedOnUtc);
        if (toUtc.HasValue)
            query = query.Where(c => toUtc.Value >= c.CreatedOnUtc);
        if (!string.IsNullOrEmpty(message))
            query = query.Where(c => c.Title.Contains(message) || c.ReviewText.Contains(message));
        if (!string.IsNullOrEmpty(vendorId))
            query = query.Where(c => c.VendorId == vendorId);
        query = query.OrderByDescending(c => c.CreatedOnUtc);
        return await PagedList<VendorReview>.Create(query, pageIndex, pageSize);
    }


    /// <summary>
    ///     Update vendor review totals
    /// </summary>
    /// <param name="vendor">Vendor</param>
    public virtual async Task UpdateVendorReviewTotals(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        var approvedRatingSum = 0;
        var notApprovedRatingSum = 0;
        var approvedTotalReviews = 0;
        var notApprovedTotalReviews = 0;
        var reviews = _vendorReviewRepository.Table.Where(x => x.VendorId == vendor.Id);
        foreach (var pr in reviews)
            if (pr.IsApproved)
            {
                approvedRatingSum += pr.Rating;
                approvedTotalReviews++;
            }
            else
            {
                notApprovedRatingSum += pr.Rating;
                notApprovedTotalReviews++;
            }

        vendor.ApprovedRatingSum = approvedRatingSum;
        vendor.NotApprovedRatingSum = notApprovedRatingSum;
        vendor.ApprovedTotalReviews = approvedTotalReviews;
        vendor.NotApprovedTotalReviews = notApprovedTotalReviews;

        var update = UpdateBuilder<Vendor>.Create()
            .Set(x => x.ApprovedRatingSum, vendor.ApprovedRatingSum)
            .Set(x => x.NotApprovedRatingSum, vendor.NotApprovedRatingSum)
            .Set(x => x.ApprovedTotalReviews, vendor.ApprovedTotalReviews)
            .Set(x => x.NotApprovedTotalReviews, vendor.NotApprovedTotalReviews);

        await _vendorRepository.UpdateOneAsync(x => x.Id == vendor.Id, update);

        //event notification
        await _mediator.EntityUpdated(vendor);
    }

    public virtual async Task UpdateVendorReview(VendorReview vendorReview)
    {
        ArgumentNullException.ThrowIfNull(vendorReview);

        var update = UpdateBuilder<VendorReview>.Create()
            .Set(x => x.Title, vendorReview.Title)
            .Set(x => x.ReviewText, vendorReview.ReviewText)
            .Set(x => x.IsApproved, vendorReview.IsApproved)
            .Set(x => x.HelpfulYesTotal, vendorReview.HelpfulYesTotal)
            .Set(x => x.HelpfulNoTotal, vendorReview.HelpfulNoTotal);

        await _vendorReviewRepository.UpdateOneAsync(x => x.Id == vendorReview.Id, update);

        //event notification
        await _mediator.EntityUpdated(vendorReview);
    }

    /// <summary>
    ///     Inserts a vendor review
    /// </summary>
    /// <param name="vendorReview">Vendor review</param>
    public virtual async Task InsertVendorReview(VendorReview vendorReview)
    {
        ArgumentNullException.ThrowIfNull(vendorReview);

        await _vendorReviewRepository.InsertAsync(vendorReview);

        //event notification
        await _mediator.EntityInserted(vendorReview);
    }

    /// <summary>
    ///     Deletes a vendor review
    /// </summary>
    /// <param name="vendorReview">Vendor review</param>
    public virtual async Task DeleteVendorReview(VendorReview vendorReview)
    {
        ArgumentNullException.ThrowIfNull(vendorReview);

        await _vendorReviewRepository.DeleteAsync(vendorReview);

        //event notification
        await _mediator.EntityDeleted(vendorReview);
    }

    /// <summary>
    ///     Gets vendor review
    /// </summary>
    /// <param name="vendorReviewId">Vendor review identifier</param>
    /// <returns>Vendor review</returns>
    public virtual Task<VendorReview> GetVendorReviewById(string vendorReviewId)
    {
        return _vendorReviewRepository.GetByIdAsync(vendorReviewId);
    }


    /// <summary>
    ///     Search vendors
    /// </summary>
    /// <param name="vendorId">Vendor identifier; "" to load all records</param>
    /// <param name="keywords">Keywords</param>
    /// <returns>Vendors</returns>
    public virtual async Task<IList<Vendor>> SearchVendors(
        string vendorId = "",
        string keywords = null)
    {
        //vendors
        var query = from p in _vendorRepository.Table
            select p;

        //searching by keyword
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(p => p.Name.ToLower().Contains(keywords.ToLower()));
        //vendor filtering
        if (!string.IsNullOrEmpty(vendorId)) query = query.Where(x => x.Id == vendorId);
        return await Task.FromResult(query.ToList());
    }

    #endregion

    #endregion
}