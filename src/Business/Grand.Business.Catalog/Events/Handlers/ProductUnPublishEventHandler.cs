﻿using Grand.Business.Core.Events.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class ProductUnPublishEventHandler : INotificationHandler<ProductUnPublishEvent>
    {
        private readonly ICacheBase _cacheBase;

        public ProductUnPublishEventHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(ProductUnPublishEvent notification, CancellationToken cancellationToken)
        {
            if (notification.Product.ShowOnHomePage)
                await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_SHOWONHOMEPAGE);
        }
    }
}
