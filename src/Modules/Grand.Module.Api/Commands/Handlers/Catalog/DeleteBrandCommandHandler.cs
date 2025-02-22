using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Module.Api.Commands.Models.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly IBrandService _brandService;

    public DeleteBrandCommandHandler(
        IBrandService brandService)
    {
        _brandService = brandService;
    }

    public async Task<bool> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandService.GetBrandById(request.Model.Id);
        if (brand != null) await _brandService.DeleteBrand(brand);
        return true;
    }
}