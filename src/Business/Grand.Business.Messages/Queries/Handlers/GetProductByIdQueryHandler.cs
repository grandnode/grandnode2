using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Queries.Models.Handlers
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
