using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;
using MongoDB.Driver.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class GetGiftVoucherQueryHandler : IRequestHandler<GetGiftVoucherQuery, IMongoQueryable<GiftVoucher>>
    {
        private readonly IRepository<GiftVoucher> _giftVoucherRepository;

        public GetGiftVoucherQueryHandler(IRepository<GiftVoucher> giftVoucherRepository)
        {
            _giftVoucherRepository = giftVoucherRepository;
        }

        public Task<IMongoQueryable<GiftVoucher>> Handle(GetGiftVoucherQuery request, CancellationToken cancellationToken)
        {
            var query = _giftVoucherRepository.Table;

            if (!string.IsNullOrEmpty(request.GiftVoucherId))
                query = query.Where(gc => gc.Id == request.GiftVoucherId);

            if (!string.IsNullOrEmpty(request.PurchasedWithOrderItemId))
                query = query.Where(gc => gc.PurchasedWithOrderItem.Id == request.PurchasedWithOrderItemId);

            if (request.CreatedFromUtc.HasValue)
                query = query.Where(gc => request.CreatedFromUtc.Value <= gc.CreatedOnUtc);
            if (request.CreatedToUtc.HasValue)
                query = query.Where(gc => request.CreatedToUtc.Value >= gc.CreatedOnUtc);
            if (request.IsGiftVoucherActivated.HasValue)
                query = query.Where(gc => gc.IsGiftVoucherActivated == request.IsGiftVoucherActivated.Value);
            if (!string.IsNullOrEmpty(request.Code))
            {
                query = query.Where(gc => gc.Code == request.Code.ToLowerInvariant());
            }
            if (!string.IsNullOrWhiteSpace(request.RecipientName))
                query = query.Where(c => c.RecipientName.Contains(request.RecipientName));
            query = query.OrderByDescending(gc => gc.CreatedOnUtc);

            return Task.FromResult(query);
        }
    }
}
