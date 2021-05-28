using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Orders
{
    /// <summary>
    /// Merchandise return service
    /// </summary>
    public partial class MerchandiseReturnService : IMerchandiseReturnService
    {
        #region Fields
        private static readonly Object _locker = new object();

        private readonly IRepository<MerchandiseReturn> _merchandiseReturnRepository;
        private readonly IRepository<MerchandiseReturnAction> _merchandiseReturnActionRepository;
        private readonly IRepository<MerchandiseReturnReason> _merchandiseReturnReasonRepository;
        private readonly IRepository<MerchandiseReturnNote> _merchandiseReturnNoteRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="merchandiseReturnRepository">Merchandise return repository</param>
        /// <param name="merchandiseReturnActionRepository">Merchandise return action repository</param>
        /// <param name="merchandiseReturnReasonRepository">Merchandise return reason repository</param>
        /// <param name="merchandiseReturnNoteRepository">Merchandise return note repository</param>
        /// <param name="_cacheBase">Cache base</param>
        /// <param name="mediator">Mediator</param>
        public MerchandiseReturnService(IRepository<MerchandiseReturn> merchandiseReturnRepository,
            IRepository<MerchandiseReturnAction> merchandiseReturnActionRepository,
            IRepository<MerchandiseReturnReason> merchandiseReturnReasonRepository,
            IRepository<MerchandiseReturnNote> merchandiseReturnNoteRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _merchandiseReturnRepository = merchandiseReturnRepository;
            _merchandiseReturnActionRepository = merchandiseReturnActionRepository;
            _merchandiseReturnReasonRepository = merchandiseReturnReasonRepository;
            _merchandiseReturnNoteRepository = merchandiseReturnNoteRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #endregion

        #region Methods

       
        /// <summary>
        /// Gets a merchandise return
        /// </summary>
        /// <param name="merchandiseReturnId">Merchandise return identifier</param>
        /// <returns>Merchandise return</returns>
        public virtual Task<MerchandiseReturn> GetMerchandiseReturnById(string merchandiseReturnId)
        {
            return _merchandiseReturnRepository.GetByIdAsync(merchandiseReturnId);
        }

        /// <summary>
        /// Gets a merchandise return
        /// </summary>
        /// <param name="id">Merchandise return identifier</param>
        /// <returns>Merchandise return</returns>
        public virtual Task<MerchandiseReturn> GetMerchandiseReturnById(int id)
        {
            return Task.FromResult(_merchandiseReturnRepository.Table.Where(x => x.ReturnNumber == id).FirstOrDefault());
        }

        /// <summary>
        /// Search merchandise returns
        /// </summary>
        /// <param name="storeId">Store identifier; 0 to load all entries</param>
        /// <param name="customerId">Customer identifier; null to load all entries</param>
        /// <param name="orderItemId">Order item identifier; 0 to load all entries</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="ownerId">Owner identifier</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="rs">Merchandise return status; null to load all entries</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Merchandise returns</returns>
        public virtual async Task<IPagedList<MerchandiseReturn>> SearchMerchandiseReturns(string storeId = "", string customerId = "",
            string orderItemId = "", string vendorId = "", string ownerId = "", MerchandiseReturnStatus? rs = null,
            int pageIndex = 0, int pageSize = int.MaxValue, DateTime? createdFromUtc = null, DateTime? createdToUtc = null)
        {
            var model = new GetMerchandiseReturnQuery()
            {
                CreatedFromUtc = createdFromUtc,
                CreatedToUtc = createdToUtc,
                PageIndex = pageIndex,
                PageSize = pageSize,
                CustomerId = customerId,
                VendorId = vendorId,
                OwnerId = ownerId,
                StoreId = storeId,
                OrderItemId = orderItemId,
                Rs = rs,
            };

            var query = await _mediator.Send(model);
            return await PagedList<MerchandiseReturn>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets all merchandise return actions
        /// </summary>
        /// <returns>Merchandise return actions</returns>
        public virtual async Task<IList<MerchandiseReturnAction>> GetAllMerchandiseReturnActions()
        {
            return await _cacheBase.GetAsync(CacheKey.MERCHANDISE_RETURN_ACTIONS_ALL_KEY, async () =>
            {
                var query = from rra in _merchandiseReturnActionRepository.Table
                            orderby rra.DisplayOrder
                            select rra;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnActionId">Merchandise return action identifier</param>
        /// <returns>Merchandise return action</returns>
        public virtual Task<MerchandiseReturnAction> GetMerchandiseReturnActionById(string merchandiseReturnActionId)
        {
            return _merchandiseReturnActionRepository.GetByIdAsync(merchandiseReturnActionId);
        }

        /// <summary>
        /// Inserts a merchandise return 
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return </param>
        public virtual async Task InsertMerchandiseReturn(MerchandiseReturn merchandiseReturn)
        {
            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            lock (_locker)
            {
                var requestExists = _merchandiseReturnRepository.Table.FirstOrDefault();
                var requestNumber = requestExists != null ? _merchandiseReturnRepository.Table.Max(x => x.ReturnNumber) + 1 : 1;
                merchandiseReturn.ReturnNumber = requestNumber;
            }
            await _merchandiseReturnRepository.InsertAsync(merchandiseReturn);

            //event notification
            await _mediator.EntityInserted(merchandiseReturn);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="merchandiseReturn"></param>
        public virtual async Task UpdateMerchandiseReturn(MerchandiseReturn merchandiseReturn)
        {
            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            await _merchandiseReturnRepository.UpdateAsync(merchandiseReturn);

            //event notification
            await _mediator.EntityUpdated(merchandiseReturn);
        }
        /// <summary>
        /// Deletes a merchandise return
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        public virtual async Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn)
        {
            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            await _merchandiseReturnRepository.DeleteAsync(merchandiseReturn);

            //event notification
            await _mediator.EntityDeleted(merchandiseReturn);
        }

        /// <summary>
        /// Inserts a merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnAction">Merchandise return action</param>
        public virtual async Task InsertMerchandiseReturnAction(MerchandiseReturnAction merchandiseReturnAction)
        {
            if (merchandiseReturnAction == null)
                throw new ArgumentNullException(nameof(merchandiseReturnAction));

            await _merchandiseReturnActionRepository.InsertAsync(merchandiseReturnAction);

            //event notification
            await _mediator.EntityInserted(merchandiseReturnAction);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_ALL_KEY);
        }
        /// <summary>
        /// Updates the  merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnAction">Merchandise return action</param>
        public virtual async Task UpdateMerchandiseReturnAction(MerchandiseReturnAction merchandiseReturnAction)
        {
            if (merchandiseReturnAction == null)
                throw new ArgumentNullException(nameof(merchandiseReturnAction));

            await _merchandiseReturnActionRepository.UpdateAsync(merchandiseReturnAction);

            //event notification
            await _mediator.EntityUpdated(merchandiseReturnAction);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_ALL_KEY);

        }
        /// <summary>
        /// Delete a merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnAction">Merchandise return action</param>
        public virtual async Task DeleteMerchandiseReturnAction(MerchandiseReturnAction merchandiseReturnAction)
        {
            if (merchandiseReturnAction == null)
                throw new ArgumentNullException(nameof(merchandiseReturnAction));

            await _merchandiseReturnActionRepository.DeleteAsync(merchandiseReturnAction);

            //event notification
            await _mediator.EntityDeleted(merchandiseReturnAction);
        }
        /// <summary>
        /// Delete a merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReason">Merchandise return reason</param>
        public virtual async Task DeleteMerchandiseReturnReason(MerchandiseReturnReason merchandiseReturnReason)
        {
            if (merchandiseReturnReason == null)
                throw new ArgumentNullException(nameof(merchandiseReturnReason));

            await _merchandiseReturnReasonRepository.DeleteAsync(merchandiseReturnReason);

            //event notification
            await _mediator.EntityDeleted(merchandiseReturnReason);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_ALL_KEY);

        }

        /// <summary>
        /// Gets all merchandise return reaspns
        /// </summary>
        /// <returns>Merchandise return reaspns</returns>
        public virtual async Task<IList<MerchandiseReturnReason>> GetAllMerchandiseReturnReasons()
        {
            return await _cacheBase.GetAsync(CacheKey.MERCHANDISE_RETURN_REASONS_ALL_KEY, async () =>
            {
                var query = from rra in _merchandiseReturnReasonRepository.Table
                            orderby rra.DisplayOrder
                            select rra;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReasonId">Merchandise return reaspn identifier</param>
        /// <returns>Merchandise return reaspn</returns>
        public virtual Task<MerchandiseReturnReason> GetMerchandiseReturnReasonById(string merchandiseReturnReasonId)
        {
            return _merchandiseReturnReasonRepository.GetByIdAsync(merchandiseReturnReasonId);
        }

        /// <summary>
        /// Inserts a merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReason">Merchandise return reaspn</param>
        public virtual async Task InsertMerchandiseReturnReason(MerchandiseReturnReason merchandiseReturnReason)
        {
            if (merchandiseReturnReason == null)
                throw new ArgumentNullException(nameof(merchandiseReturnReason));

            await _merchandiseReturnReasonRepository.InsertAsync(merchandiseReturnReason);

            //event notification
            await _mediator.EntityInserted(merchandiseReturnReason);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_ALL_KEY);
        }

        /// <summary>
        /// Updates the  merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReason">Merchandise return reaspn</param>
        public virtual async Task UpdateMerchandiseReturnReason(MerchandiseReturnReason merchandiseReturnReason)
        {
            if (merchandiseReturnReason == null)
                throw new ArgumentNullException(nameof(merchandiseReturnReason));

            await _merchandiseReturnReasonRepository.UpdateAsync(merchandiseReturnReason);

            //event notification
            await _mediator.EntityUpdated(merchandiseReturnReason);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_ALL_KEY);

        }

        #region Merchandise return notes

        /// <summary>
        /// Deletes a merchandise return note
        /// </summary>
        /// <param name="merchandiseReturnNote">The merchandise return note</param>
        public virtual async Task DeleteMerchandiseReturnNote(MerchandiseReturnNote merchandiseReturnNote)
        {
            if (merchandiseReturnNote == null)
                throw new ArgumentNullException(nameof(merchandiseReturnNote));

            await _merchandiseReturnNoteRepository.DeleteAsync(merchandiseReturnNote);

            //event notification
            await _mediator.EntityDeleted(merchandiseReturnNote);
        }

        /// <summary>
        /// Inserts a merchandise return note
        /// </summary>
        /// <param name="merchandiseReturnNote">The merchandise return note</param>
        public virtual async Task InsertMerchandiseReturnNote(MerchandiseReturnNote merchandiseReturnNote)
        {
            if (merchandiseReturnNote == null)
                throw new ArgumentNullException(nameof(merchandiseReturnNote));

            await _merchandiseReturnNoteRepository.InsertAsync(merchandiseReturnNote);

            //event notification
            await _mediator.EntityInserted(merchandiseReturnNote);
        }

        /// <summary>
        /// Get notes related to merchandise return
        /// </summary>
        /// <param name="merchandiseReturnId">The merchandise return identifier</param>
        /// <returns>List of merchandise return notes</returns>
        public virtual async Task<IList<MerchandiseReturnNote>> GetMerchandiseReturnNotes(string merchandiseReturnId)
        {
            var query = from merchandiseReturnNote in _merchandiseReturnNoteRepository.Table
                        where merchandiseReturnNote.MerchandiseReturnId == merchandiseReturnId
                        orderby merchandiseReturnNote.CreatedOnUtc descending
                        select merchandiseReturnNote;

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Get merchandise return note by id
        /// </summary>
        /// <param name="merchandiseReturnNoteId">The merchandise return note identifier</param>
        /// <returns>MerchandiseReturnNote</returns>
        public virtual Task<MerchandiseReturnNote> GetMerchandiseReturnNote(string merchandiseReturnNoteId)
        {
            return Task.FromResult(_merchandiseReturnNoteRepository.Table.Where(x => x.Id == merchandiseReturnNoteId).FirstOrDefault());
        }

        #endregion

        #endregion
    }
}
