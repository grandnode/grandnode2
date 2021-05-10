using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteBrandCommand : IRequest<bool>
    {
        public BrandDto Model { get; set; }
    }
}
