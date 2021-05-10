using Grand.Domain;
using Grand.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Orders
{
    /// <summary>
    /// Merchandise return service interface
    /// </summary>
    public partial interface IMerchandiseReturnService
    {
        
        /// <summary>
        /// Gets a merchandise return
        /// </summary>
        /// <param name="merchandiseReturnId">Merchandise return identifier</param>
        /// <returns>Merchandise return</returns>
        Task<MerchandiseReturn> GetMerchandiseReturnById(string merchandiseReturnId);

        /// <summary>
        /// Gets a merchandise return
        /// </summary>
        /// <param name="id">Merchandise return number</param>t
        /// <returns>Merchandise return</returns>
        Task<MerchandiseReturn> GetMerchandiseReturnById(int id);

        /// <summary>
        /// Search merchandise returns
        /// </summary>
        /// <param name="storeId">Store identifier; 0 to load all entries</param>
        /// <param name="customerId">Customer identifier; 0 to load all entries</param>
        /// <param name="orderItemId">Order item identifier; 0 to load all entries</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="ownerId">Owner identifier</param>
        /// <param name="rs">Merchandise return status; null to load all entries</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Merchandise returns</returns>
        Task<IPagedList<MerchandiseReturn>> SearchMerchandiseReturns(string storeId = "", string customerId = "",
            string orderItemId = "", string vendorId = "", string ownerId = "", MerchandiseReturnStatus? rs = null,
            int pageIndex = 0, int pageSize = int.MaxValue, DateTime? createdFromUtc = null, DateTime? createdToUtc = null);

        /// <summary>
        /// Inserts a merchandise return
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return </param>
        Task InsertMerchandiseReturn(MerchandiseReturn merchandiseReturn);
        /// <summary>
        /// Update the merchandise return
        /// </summary>
        /// <param name="merchandiseReturn"></param>
        Task UpdateMerchandiseReturn(MerchandiseReturn merchandiseReturn);
        /// <summary>
        /// Deletes a merchandise return
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn);

        /// <summary>
        /// Gets all merchandise return actions
        /// </summary>
        /// <returns>Merchandise return actions</returns>
        Task<IList<MerchandiseReturnAction>> GetAllMerchandiseReturnActions();

        /// <summary>
        /// Gets a merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnActionId">Merchandise return action identifier</param>
        /// <returns>Merchandise return action</returns>
        Task<MerchandiseReturnAction> GetMerchandiseReturnActionById(string merchandiseReturnActionId);

        /// <summary>
        /// Inserts a merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnAction">Merchandise return action</param>
        Task InsertMerchandiseReturnAction(MerchandiseReturnAction merchandiseReturnAction);

        /// <summary>
        /// Delete a merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnAction">Merchandise return action</param>
        Task DeleteMerchandiseReturnAction(MerchandiseReturnAction merchandiseReturnAction);

        /// <summary>
        /// Updates the  merchandise return action
        /// </summary>
        /// <param name="merchandiseReturnAction">Merchandise return action</param>
        Task UpdateMerchandiseReturnAction(MerchandiseReturnAction merchandiseReturnAction);

        /// <summary>
        /// Gets all merchandise return reaspns
        /// </summary>
        /// <returns>Merchandise return reaspns</returns>
        Task<IList<MerchandiseReturnReason>> GetAllMerchandiseReturnReasons();

        /// <summary>
        /// Gets a merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReasonId">Merchandise return reason identifier</param>
        /// <returns>Merchandise return reaspn</returns>
        Task<MerchandiseReturnReason> GetMerchandiseReturnReasonById(string merchandiseReturnReasonId);

        /// <summary>
        /// Inserts a merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReason">Merchandise return reason</param>
        Task InsertMerchandiseReturnReason(MerchandiseReturnReason merchandiseReturnReason);

        /// <summary>
        /// Updates the merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReason">Merchandise return reason</param>
        Task UpdateMerchandiseReturnReason(MerchandiseReturnReason merchandiseReturnReason);
        /// <summary>
        /// Delete a merchandise return reaspn
        /// </summary>
        /// <param name="merchandiseReturnReason">Merchandise return reason</param>
        Task DeleteMerchandiseReturnReason(MerchandiseReturnReason merchandiseReturnReason);

        #region Merchandise return notes


        /// <summary>
        /// Insert a merchandise return note
        /// </summary>
        /// <param name="merchandiseReturnNote">The merchandise return note</param>
        Task InsertMerchandiseReturnNote(MerchandiseReturnNote merchandiseReturnNote);
        /// <summary>
        /// Deletes a merchandise return note
        /// </summary>
        /// <param name="merchandiseReturnNote">The merchandise return note</param>
        Task DeleteMerchandiseReturnNote(MerchandiseReturnNote merchandiseReturnNote);

        /// <summary>
        /// Get notes for merchandise return
        /// </summary>
        /// <param name="merchandiseReturnId">Merchandise return identifier</param>
        /// <returns>MerchandiseReturnNote</returns>
        Task<IList<MerchandiseReturnNote>> GetMerchandiseReturnNotes(string merchandiseReturnId);

        /// <summary>
        /// Get merchandise return note by id
        /// </summary>
        /// <param name="merchandiseReturnNoteId">Merchandise return note identifier</param>
        /// <returns>MerchandiseReturnNote</returns>
        Task<MerchandiseReturnNote> GetMerchandiseReturnNote(string merchandiseReturnNoteId);

        #endregion
    }
}
