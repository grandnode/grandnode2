using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteProductCategoryCommand : IRequest<bool>
{
    public required ProductDto Product { get; set; }
    public required string CategoryId { get; set; }
}