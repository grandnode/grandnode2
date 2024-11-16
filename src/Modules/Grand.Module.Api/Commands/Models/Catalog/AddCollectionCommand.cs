using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddCollectionCommand : IRequest<CollectionDto>
{
    public CollectionDto Model { get; set; }
}