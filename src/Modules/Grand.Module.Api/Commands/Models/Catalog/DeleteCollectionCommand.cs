using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteCollectionCommand : IRequest<bool>
{
    public CollectionDto Model { get; set; }
}