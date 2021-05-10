using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductCollectionCommand : IRequest<bool>
    {
        public ProductDto Product { get; set; }
        public ProductCollectionDto Model { get; set; }
    }
}
