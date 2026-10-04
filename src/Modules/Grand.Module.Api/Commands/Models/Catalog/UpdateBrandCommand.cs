using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateBrandCommand : IRequest<BrandDto>
{
    public BrandDto Model { get; set; }
}