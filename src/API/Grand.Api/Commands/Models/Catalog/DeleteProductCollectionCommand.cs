using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductCollectionCommand : IRequest<bool>
    {
        public ProductDto Product { get; set; }
        public string CollectionId { get; set; }
    }
}
