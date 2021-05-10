using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductCollectionCommand : IRequest<bool>
    {
        public ProductDto Product { get; set; }
        public ProductCollectionDto Model { get; set; }
    }
}
