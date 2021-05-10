using Grand.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddBrandCommand : IRequest<BrandDto>
    {
        public BrandDto Model { get; set; }
    }
}
