using Grand.Infrastructure.Caching;
using Grand.Domain.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Events;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using Grand.Infrastructure.Caching.Constants;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class DiscountDeletedEventHandler : INotificationHandler<EntityDeleted<Discount>>
    {
        
        #region Fields

        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Collection> _collectionRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<DiscountCoupon> _discountCouponRepository;
        private readonly ICacheBase _cacheBase;

        #endregion

        public DiscountDeletedEventHandler(
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IRepository<Collection> collectionRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<DiscountCoupon> discountCouponRepository,
            ICacheBase cacheBase
        )
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _collectionRepository = collectionRepository;
            _vendorRepository = vendorRepository;
            _vendorRepository = vendorRepository;
            _discountCouponRepository = discountCouponRepository;
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityDeleted<Discount> notification, CancellationToken cancellationToken)
        {
            var discount = notification.Entity;

            var builder = Builders<BsonDocument>.Filter;
            if (discount.DiscountTypeId == DiscountType.AssignedToSkus)
            {
                var builderproduct = Builders<Product>.Update;
                var updatefilter = builderproduct.Pull(x => x.AppliedDiscounts, discount.Id);
                await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);
                await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
            }

            if (discount.DiscountTypeId == DiscountType.AssignedToCategories)
            {
                var buildercategory = Builders<Category>.Update;
                var updatefilter = buildercategory.Pull(x => x.AppliedDiscounts, discount.Id);
                await _categoryRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);
                await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
            }
            if (discount.DiscountTypeId == DiscountType.AssignedToBrands)
            {
                var builderbrand = Builders<Brand>.Update;
                var updatefilter = builderbrand.Pull(x => x.AppliedDiscounts, discount.Id);
                await _brandRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);
                await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);
            }
            if (discount.DiscountTypeId == DiscountType.AssignedToCollections)
            {
                var buildercollection = Builders<Collection>.Update;
                var updatefilter = buildercollection.Pull(x => x.AppliedDiscounts, discount.Id);
                await _collectionRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);
                await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
            }
            if (discount.DiscountTypeId == DiscountType.AssignedToVendors)
            {
                var buildervendor = Builders<Vendor>.Update;
                var updatefilter = buildervendor.Pull(x => x.AppliedDiscounts, discount.Id);
                await _vendorRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);
                await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
            }
            
            //remove coupon codes
            var filtersCoupon = Builders<DiscountCoupon>.Filter;
            var filterCrp = filtersCoupon.Eq(x => x.DiscountId, discount.Id);

            await _discountCouponRepository.Collection.DeleteManyAsync(filterCrp);
        }
    }
}
