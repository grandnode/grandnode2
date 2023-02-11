﻿using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Business.Catalog.Services.Products
{
    /// <summary>
    /// Auction service
    /// </summary>
    public class AuctionService : IAuctionService
    {
        private readonly IRepository<Bid> _bidRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        public AuctionService(IRepository<Bid> bidRepository,
            IRepository<Product> productRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _bidRepository = bidRepository;
            _productRepository = productRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        public virtual Task<Bid> GetBid(string id)
        {
            return _bidRepository.GetByIdAsync(id);
        }

        public virtual async Task<Bid> GetLatestBid(string productId)
        {
            var bid = _bidRepository.Table
                            .Where(x=>x.ProductId == productId)
                            .OrderByDescending(x => x.Date)
                            .ToList();

            return await Task.FromResult(bid.FirstOrDefault());
        }

        public virtual async Task<IPagedList<Bid>> GetBidsByProductId(string productId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _bidRepository.Table.Where(x => x.ProductId == productId).OrderByDescending(x => x.Date);
            return await Task.FromResult(new PagedList<Bid>(query, pageIndex, pageSize));
        }

        public virtual async Task<IPagedList<Bid>> GetBidsByCustomerId(string customerId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _bidRepository.Table.Where(x => x.CustomerId == customerId);
            return await Task.FromResult(new PagedList<Bid>(query, pageIndex, pageSize));
        }

        public virtual async Task InsertBid(Bid bid)
        {
            if (bid == null)
                throw new ArgumentNullException(nameof(bid));

            await _bidRepository.InsertAsync(bid);
            await _mediator.EntityInserted(bid);
        }

        public virtual async Task UpdateBid(Bid bid)
        {
            if (bid == null)
                throw new ArgumentNullException(nameof(bid));

            await _bidRepository.UpdateAsync(bid);
            await _mediator.EntityUpdated(bid);
        }
        public virtual async Task DeleteBid(Bid bid)
        {
            if (bid == null)
                throw new ArgumentNullException(nameof(bid));

            await _bidRepository.DeleteAsync(bid);
            await _mediator.EntityDeleted(bid);

            var productToUpdate = await _productRepository.GetByIdAsync(bid.ProductId);
            var bids = await GetBidsByProductId(bid.ProductId);
            var highestBid = bids.MaxBy(x => x.Amount);
            if (productToUpdate != null)
            {
                await UpdateHighestBid(productToUpdate, highestBid?.Amount ?? 0, highestBid != null ? highestBid.CustomerId : "");
            }
        }
        public virtual async Task UpdateHighestBid(Product product, double bid, string highestBidder)
        {
            product.HighestBid = bid;
            product.HighestBidder = highestBidder;
            product.UpdatedOnUtc = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);

            await _cacheBase.RemoveAsync(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            await _mediator.EntityUpdated(product);
        }

        public virtual async Task<IList<Product>> GetAuctionsToEnd()
        {
            return await Task.FromResult(_productRepository.Table
                .Where(x => x.ProductTypeId == ProductType.Auction && 
                        !x.AuctionEnded && x.AvailableEndDateTimeUtc < DateTime.UtcNow).ToList());
        }

        public virtual async Task UpdateAuctionEnded(Product product, bool ended, bool endDate = false)
        {
            product.AuctionEnded = ended;
            product.UpdatedOnUtc = DateTime.UtcNow;
            if (endDate)
                product.AvailableEndDateTimeUtc = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);

            await _cacheBase.RemoveAsync(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            await _mediator.EntityUpdated(product);
        }
        /// <summary>
        /// New bid
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="product"></param>
        /// <param name="store"></param>
        /// <param name="warehouseId"></param>
        /// <param name="language"></param>
        /// <param name="amount"></param>
        public virtual async Task NewBid(Customer customer, Product product, Store store, Language language, string warehouseId, double amount)
        {
            var latest = await GetLatestBid(product.Id);
            await InsertBid(new Bid
            {
                Date = DateTime.UtcNow,
                Amount = amount,
                CustomerId = customer.Id,
                ProductId = product.Id,
                StoreId = store.Id,
                WarehouseId = warehouseId,
            });

            if (latest != null)
            {
                if (latest.CustomerId != customer.Id)
                {
                    await _mediator.Send(new SendOutBidCustomerCommand()
                    {
                        Product = product,
                        Bid = latest,
                        Language = language
                    });
                }
            }
            product.HighestBid = amount;
            await UpdateHighestBid(product, amount, customer.Id);
        }

        /// <summary>
        /// Cancel bid
        /// </summary>
        /// <param name="orderId">OrderId</param>
        public virtual async Task CancelBidByOrder(string orderId)
        {
            var bid = _bidRepository.Table.FirstOrDefault(x => x.OrderId == orderId);
            if (bid != null)
            {
                await _bidRepository.DeleteAsync(bid);
                var product = await _productRepository.GetByIdAsync(bid.ProductId);
                if (product != null)
                {
                    await UpdateHighestBid(product, 0, "");
                    await UpdateAuctionEnded(product, false);
                }
            }
        }
    }
}