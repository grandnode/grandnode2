using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteBrandCommand : IRequest<bool>
{
    public BrandDto Model { get; set; }
}