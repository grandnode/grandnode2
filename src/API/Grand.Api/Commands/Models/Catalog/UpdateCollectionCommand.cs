using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateCollectionCommand : IRequest<CollectionDto>
    {
        public CollectionDto Model { get; set; }
    }
}
