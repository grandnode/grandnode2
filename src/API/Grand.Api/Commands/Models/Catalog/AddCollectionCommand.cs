using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddCollectionCommand : IRequest<CollectionDto>
    {
        public CollectionDto Model { get; set; }
    }
}
