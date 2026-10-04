using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateCollectionCommand : IRequest<CollectionDto>
{
    public CollectionDto Model { get; set; }
}