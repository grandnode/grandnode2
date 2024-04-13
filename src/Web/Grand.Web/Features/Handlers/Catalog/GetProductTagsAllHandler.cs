using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetProductTagsAllHandler : IRequestHandler<GetProductTagsAll, PopularProductTagsModel>
{
    private readonly IProductTagService _productTagService;

    public GetProductTagsAllHandler(IProductTagService productTagService)
    {
        _productTagService = productTagService;
    }

    public async Task<PopularProductTagsModel> Handle(GetProductTagsAll request, CancellationToken cancellationToken)
    {
        var model = new PopularProductTagsModel();
        model.Tags = (await _productTagService
                .GetAllProductTags())
            .OrderBy(x => x.Name)
            .Select(x =>
            {
                var ptModel = new ProductTagModel {
                    Id = x.Id,
                    Name = x.GetTranslation(y => y.Name, request.Language.Id),
                    SeName = x.SeName,
                    ProductCount = x.Count
                };
                return ptModel;
            }).ToList();
        return model;
    }
}