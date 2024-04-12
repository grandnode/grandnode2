using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetProductArchByIdQueryHandler : IRequestHandler<GetProductArchByIdQuery, Product>
{
    private readonly IRepository<ProductDeleted> _productDeletedRepository;

    public GetProductArchByIdQueryHandler(IRepository<ProductDeleted> productDeletedRepository)
    {
        _productDeletedRepository = productDeletedRepository;
    }

    public async Task<Product> Handle(GetProductArchByIdQuery request, CancellationToken cancellationToken)
    {
        return await _productDeletedRepository.GetByIdAsync(request.Id);
    }
}