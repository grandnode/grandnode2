using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddProductPictureCommand : IRequest<bool>
{
    public ProductDto Product { get; set; }
    public ProductPictureDto Model { get; set; }
}