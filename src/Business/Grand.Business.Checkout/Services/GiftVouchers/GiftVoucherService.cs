using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.GiftVouchers
{
    /// <summary>
    /// Gift voucher service
    /// </summary>
    public partial class GiftVoucherService : IGiftVoucherService
    {
        #region Fields

        private readonly IRepository<GiftVoucher> _giftVoucherRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="giftVoucherRepository">Gift voucher context</param>
        /// <param name="mediator">Mediator</param>
        public GiftVoucherService(IRepository<GiftVoucher> giftVoucherRepository, IMediator mediator)
        {
            _giftVoucherRepository = giftVoucherRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        
        /// <summary>
        /// Gets a gift voucher
        /// </summary>
        /// <param name="giftVoucherId">Gift voucher identifier</param>
        /// <returns>Gift voucher entry</returns>
        public virtual Task<GiftVoucher> GetGiftVoucherById(string giftVoucherId)
        {
            return _giftVoucherRepository.GetByIdAsync(giftVoucherId);
        }

        /// <summary>
        /// Gets all gift vouchers
        /// </summary>
        /// <param name="purchasedWithOrderId">Associated order ID; null to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="isGiftVoucherActivated">Value indicating whether gift voucher is activated; null to load all records</param>
        /// <param name="giftVoucherCouponCode">Gift voucher coupon code; nullto load all records</param>
        /// <param name="recipientName">Recipient name; null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Gift vouchers</returns>
        public virtual async Task<IPagedList<GiftVoucher>> GetAllGiftVouchers(string purchasedWithOrderItemId = "",
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            bool? isGiftVoucherActivated = null, string giftVoucherCouponCode = null,
            string recipientName = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var model = new GetGiftVoucherQuery()
            {
                CreatedFromUtc = createdFromUtc,
                CreatedToUtc = createdToUtc,
                Code = giftVoucherCouponCode,
                IsGiftVoucherActivated = isGiftVoucherActivated,
                PageIndex = pageIndex,
                PageSize = pageSize,
                PurchasedWithOrderItemId = purchasedWithOrderItemId,
                RecipientName = recipientName
            };

            var query = await _mediator.Send(model);
            return await PagedList<GiftVoucher>.Create(query, pageIndex, pageSize);
        }

        public virtual async Task<IList<GiftVoucherUsageHistory>> GetAllGiftVoucherUsageHistory(string orderId = "")
        {
            var query = from g in _giftVoucherRepository.Table
                        from h in g.GiftVoucherUsageHistory
                        select h;

            query = query.Where(x => x.UsedWithOrderId == orderId);
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Inserts a gift voucher
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        public virtual async Task InsertGiftVoucher(GiftVoucher giftVoucher)
        {
            if (giftVoucher == null)
                throw new ArgumentNullException(nameof(giftVoucher));
            giftVoucher.Code = giftVoucher.Code.ToLowerInvariant();
            await _giftVoucherRepository.InsertAsync(giftVoucher);

            //event notification
            await _mediator.EntityInserted(giftVoucher);
        }

        /// <summary>
        /// Updates the gift voucher
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        public virtual async Task UpdateGiftVoucher(GiftVoucher giftVoucher)
        {
            if (giftVoucher == null)
                throw new ArgumentNullException(nameof(giftVoucher));

            giftVoucher.Code = giftVoucher.Code.ToLowerInvariant();
            await _giftVoucherRepository.UpdateAsync(giftVoucher);

            //event notification
            await _mediator.EntityUpdated(giftVoucher);
        }
        /// <summary>
        /// Deletes a gift voucher
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        public virtual async Task DeleteGiftVoucher(GiftVoucher giftVoucher)
        {
            if (giftVoucher == null)
                throw new ArgumentNullException(nameof(giftVoucher));

            await _giftVoucherRepository.DeleteAsync(giftVoucher);

            //event notification
            await _mediator.EntityDeleted(giftVoucher);
        }

        /// <summary>
        /// Gets gift vouchers by 'PurchasedWithOrderItemId'
        /// </summary>
        /// <param name="purchasedWithOrderItemId">Purchased with order item identifier</param>
        /// <returns>Gift voucher entries</returns>
        public virtual async Task<IList<GiftVoucher>> GetGiftVouchersByPurchasedWithOrderItemId(string purchasedWithOrderItemId)
        {
            if (String.IsNullOrEmpty(purchasedWithOrderItemId))
                return new List<GiftVoucher>();

            var query = from p in _giftVoucherRepository.Table
                        select p;

            query = query.Where(gc => gc.PurchasedWithOrderItem.Id == purchasedWithOrderItemId);
            query = query.OrderBy(gc => gc.Id);

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Generate new gift voucher code
        /// </summary>
        /// <returns>Result</returns>
        public virtual string GenerateGiftVoucherCode()
        {
            int length = 8;
            string result = Guid.NewGuid().ToString();
            if (result.Length > length)
                result = result.Substring(0, length);
            return result;
        }

        #endregion
    }
}
