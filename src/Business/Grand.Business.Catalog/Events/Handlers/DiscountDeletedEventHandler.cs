using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Discounts;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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

            if (discount.DiscountTypeId == DiscountType.AssignedToSkus)
            {
                //delete on the product
                await _productRepository.Pull(string.Empty, x => x.AppliedDiscounts, discount.Id, true);

                await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
            }

            if (discount.DiscountTypeId == DiscountType.AssignedToCategories)
            {
                //delete on the category
                await _categoryRepository.Pull(string.Empty, x => x.AppliedDiscounts, discount.Id, true);
                //clear cache
                await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
            }
            if (discount.DiscountTypeId == DiscountType.AssignedToBrands)
            {
                //delete on the brand
                await _brandRepository.Pull(string.Empty, x => x.AppliedDiscounts, discount.Id, true);
                //clear cache
                await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);
            }
            if (discount.DiscountTypeId == DiscountType.AssignedToCollections)
            {
                //delete on the collection
                await _collectionRepository.Pull(string.Empty, x => x.AppliedDiscounts, discount.Id, true);
                //clear cache
                await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
            }
            if (discount.DiscountTypeId == DiscountType.AssignedToVendors)
            {
                //delete on the vendor
                await _vendorRepository.Pull(string.Empty, x => x.AppliedDiscounts, discount.Id, true);
                //clear cache
                await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
            }

            //remove coupon codes
            await _discountCouponRepository.DeleteManyAsync(x => x.DiscountId == discount.Id);
        }
    }
}
