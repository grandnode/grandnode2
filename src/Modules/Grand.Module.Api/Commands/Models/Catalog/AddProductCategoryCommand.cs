using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddProductCategoryCommand : IRequest<bool>
{
    public required ProductDto Product { get; set; }
    public required ProductCategoryDto Model { get; set; }
}