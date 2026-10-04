using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductCategoryCommand : IRequest<bool>
{
    public ProductDto Product { get; set; }
    public ProductCategoryDto Model { get; set; }
}