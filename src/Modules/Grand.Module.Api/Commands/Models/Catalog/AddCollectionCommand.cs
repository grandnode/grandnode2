using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddCollectionCommand : IRequest<CollectionDto>
{
    public required CollectionDto Model { get; set; }
}