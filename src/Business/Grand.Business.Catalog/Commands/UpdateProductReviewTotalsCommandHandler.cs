﻿using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Business.Catalog.Commands.Handlers
{
    public class UpdateProductReviewTotalsCommandHandler : IRequestHandler<UpdateProductReviewTotalsCommand, bool>
    {
        #region Fields

        private readonly IRepository<Product> _productRepository;
        private readonly IProductReviewService _productReviewService;
        private readonly ICacheBase _cacheBase;

        #endregion

        public UpdateProductReviewTotalsCommandHandler(IRepository<Product> productRepository, IProductReviewService productReviewService, ICacheBase cacheBase)
        {
            _productRepository = productRepository;
            _cacheBase = cacheBase;
            _productReviewService = productReviewService;
        }

        public async Task<bool> Handle(UpdateProductReviewTotalsCommand request, CancellationToken cancellationToken)
        {
            if (request.Product == null)
                throw new ArgumentNullException(nameof(request.Product));

            var approvedRatingSum = 0;
            var notApprovedRatingSum = 0;
            var approvedTotalReviews = 0;
            var notApprovedTotalReviews = 0;

            var reviews = await _productReviewService.GetAllProductReviews(null, null, null, null, null,
                null, request.Product.Id);

            foreach (var pr in reviews)
            {
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
            }

            request.Product.ApprovedRatingSum = approvedRatingSum;
            request.Product.NotApprovedRatingSum = notApprovedRatingSum;
            request.Product.ApprovedTotalReviews = approvedTotalReviews;
            request.Product.NotApprovedTotalReviews = notApprovedTotalReviews;

            var update = UpdateBuilder<Product>.Create()
            .Set(x => x.ApprovedRatingSum, request.Product.ApprovedRatingSum)
            .Set(x => x.NotApprovedRatingSum, request.Product.NotApprovedRatingSum)
            .Set(x => x.ApprovedTotalReviews, request.Product.ApprovedTotalReviews)
            .Set(x => x.NotApprovedTotalReviews, request.Product.NotApprovedTotalReviews);

            await _productRepository.UpdateOneAsync(x=>x.Id == request.Product.Id, update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, request.Product.Id));

            return true;
        }
    }
}
