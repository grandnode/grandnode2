using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Shipping
{
    /// <summary>
    /// Shipment service
    /// </summary>
    public partial class ShipmentService : IShipmentService
    {
        #region Fields

        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<ShipmentNote> _shipmentNoteRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="shipmentRepository">Shipment repository</param>
        /// <param name="shipmentNoteRepository">Order note repository</param>
        /// <param name="mediator">Mediator</param>
        public ShipmentService(IRepository<Shipment> shipmentRepository, IRepository<ShipmentNote> shipmentNoteRepository,
            IMediator mediator)
        {
            _shipmentRepository = shipmentRepository;
            _shipmentNoteRepository = shipmentNoteRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Search shipments
        /// </summary>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="warehouseId">Warehouse identifier, only shipments with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="shippingCountryId">Shipping country identifier; "" to load all records</param>
        /// <param name="shippingStateId">Shipping state identifier; "" to load all records</param>
        /// <param name="shippingCity">Shipping city; null to load all records</param>
        /// <param name="trackingNumber">Search by tracking number</param>
        /// <param name="loadNotShipped">A value indicating whether we should load only not shipped shipments</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Shipments</returns>
        public virtual async Task<IPagedList<Shipment>> GetAllShipments(string storeId = "", string vendorId = "", string warehouseId = "",
            string shippingCountryId = "",
            int shippingStateId = 0,
            string shippingCity = null,
            string trackingNumber = null,
            bool loadNotShipped = false,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _shipmentRepository.Table
                        select p;

            if (!string.IsNullOrEmpty(storeId))
            {
                query = query.Where(x => x.StoreId == storeId);
            }
            if (!string.IsNullOrEmpty(vendorId))
            {
                query = query.Where(x => x.VendorId == vendorId);
            }
            if (!string.IsNullOrEmpty(trackingNumber))
                query = query.Where(s => s.TrackingNumber.Contains(trackingNumber));

            if (loadNotShipped)
                query = query.Where(s => !s.ShippedDateUtc.HasValue);
            if (createdFromUtc.HasValue)
                query = query.Where(s => createdFromUtc.Value <= s.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(s => createdToUtc.Value >= s.CreatedOnUtc);

            query = query.OrderByDescending(x => x.CreatedOnUtc);
            var shipments = await PagedList<Shipment>.Create(query, pageIndex, pageSize);
            return shipments;

        }

        /// <summary>
        /// Get shipment by identifiers
        /// </summary>
        /// <param name="shipmentIds">Shipment identifiers</param>
        /// <returns>Shipments</returns>
        public virtual async Task<IList<Shipment>> GetShipmentsByIds(string[] shipmentIds)
        {
            if (shipmentIds == null || shipmentIds.Length == 0)
                return new List<Shipment>();

            var query = from o in _shipmentRepository.Table
                        where shipmentIds.Contains(o.Id)
                        select o;
            var shipments = await Task.FromResult(query.ToList());

            //sort by passed identifiers
            var sortedOrders = new List<Shipment>();
            foreach (string id in shipmentIds)
            {
                var shipment = shipments.Find(x => x.Id == id);
                if (shipment != null)
                    sortedOrders.Add(shipment);
            }
            return sortedOrders;
        }


        public virtual async Task<IList<Shipment>> GetShipmentsByOrder(string orderId)
        {
            return await Task.FromResult(_shipmentRepository.Table.Where(x => x.OrderId == orderId).ToList());
        }

        /// <summary>
        /// Gets a shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>Shipment</returns>
        public virtual Task<Shipment> GetShipmentById(string shipmentId)
        {
            return _shipmentRepository.GetByIdAsync(shipmentId);
        }

        /// <summary>
        /// Inserts a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        public virtual async Task InsertShipment(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));
            var shipmentExists = _shipmentRepository.Table.FirstOrDefault();
            shipment.ShipmentNumber = shipmentExists != null ? _shipmentRepository.Table.Max(x => x.ShipmentNumber) + 1 : 1;
            await _shipmentRepository.InsertAsync(shipment);

            //event notification
            await _mediator.EntityInserted(shipment);
        }

        /// <summary>
        /// Updates the shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        public virtual async Task UpdateShipment(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            await _shipmentRepository.UpdateAsync(shipment);

            //event notification
            await _mediator.EntityUpdated(shipment);
        }

        /// <summary>
        /// Deletes a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        public virtual async Task DeleteShipment(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            await _shipmentRepository.DeleteAsync(shipment);

            //event notification
            await _mediator.EntityDeleted(shipment);
        }

        #region Shipment notes

        /// <summary>
        /// Deletes an order note
        /// </summary>
        /// <param name="shipmentNote">The order note</param>
        public virtual async Task DeleteShipmentNote(ShipmentNote shipmentNote)
        {
            if (shipmentNote == null)
                throw new ArgumentNullException(nameof(shipmentNote));

            await _shipmentNoteRepository.DeleteAsync(shipmentNote);

            //event notification
            await _mediator.EntityDeleted(shipmentNote);
        }

        /// <summary>
        /// Deletes an shipment note
        /// </summary>
        /// <param name="shipmentNote">The shipment note</param>
        public virtual async Task InsertShipmentNote(ShipmentNote shipmentNote)
        {
            if (shipmentNote == null)
                throw new ArgumentNullException(nameof(shipmentNote));

            await _shipmentNoteRepository.InsertAsync(shipmentNote);

            //event notification
            await _mediator.EntityInserted(shipmentNote);
        }

        public virtual async Task<IList<ShipmentNote>> GetShipmentNotes(string shipmentId)
        {
            var query = from shipmentNote in _shipmentNoteRepository.Table
                        where shipmentNote.ShipmentId == shipmentId
                        orderby shipmentNote.CreatedOnUtc descending
                        select shipmentNote;

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Get shipmentnote by id
        /// </summary>
        /// <param name="shipmentnoteId">Shipment note identifier</param>
        /// <returns>shipmentNote</returns>
        public virtual Task<ShipmentNote> GetShipmentNote(string shipmentnoteId)
        {
            return Task.FromResult(_shipmentNoteRepository.Table.Where(x => x.Id == shipmentnoteId).FirstOrDefault());
        }


        #endregion
        #endregion
    }
}
