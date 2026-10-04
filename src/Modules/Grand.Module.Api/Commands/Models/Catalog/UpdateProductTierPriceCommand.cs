using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductTierPriceCommand : IRequest<bool>
{
    public ProductDto Product { get; set; }
    public ProductTierPriceDto Model { get; set; }
}