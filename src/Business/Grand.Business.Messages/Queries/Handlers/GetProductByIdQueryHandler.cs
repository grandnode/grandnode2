using Grand.Business.Core.Queries.Messages;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Business.Messages.Queries.Handlers
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Product>
    {
        private readonly IRepository<Product> _productRepository;

        public GetProductByIdQueryHandler(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Product> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
                return null;

            return await _productRepository.GetByIdAsync(request.Id);
        }
    }
}
