using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteCollectionCommand : IRequest<bool>
    {
        public CollectionDto Model { get; set; }
    }
}
