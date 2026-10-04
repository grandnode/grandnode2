using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteBrandCommand : IRequest<bool>
{
    public BrandDto Model { get; set; }
}