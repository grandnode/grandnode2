using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddBrandCommand : IRequest<BrandDto>
{
    public required BrandDto Model { get; set; }
}