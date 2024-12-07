using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddCategoryCommand : IRequest<CategoryDto>
{
    public CategoryDto Model { get; set; }
}