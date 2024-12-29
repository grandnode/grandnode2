using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly IBrandService _brandService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public DeleteBrandCommandHandler(
        IBrandService brandService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _brandService = brandService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<bool> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandService.GetBrandById(request.Model.Id);
        if (brand != null) await _brandService.DeleteBrand(brand);
        return true;
    }
}