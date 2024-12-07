using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteCategoryCommand : IRequest<bool>
{
    public CategoryDto Model { get; set; }
}