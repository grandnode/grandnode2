using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCategoryNavigationHandler : IRequestHandler<GetCategoryNavigation, CategoryNavigationModel>
{
    private readonly IMediator _mediator;
    private readonly IProductService _productService;

    public GetCategoryNavigationHandler(IProductService productService, IMediator mediator)
    {
        _productService = productService;
        _mediator = mediator;
    }

    public async Task<CategoryNavigationModel> Handle(GetCategoryNavigation request,
        CancellationToken cancellationToken)
    {
        //get active category
        var activeCategoryId = "";
        if (!string.IsNullOrEmpty(request.CurrentCategoryId))
        {
            //category details page
            activeCategoryId = request.CurrentCategoryId;
        }
        else if (!string.IsNullOrEmpty(request.CurrentProductId))
        {
            //product details page
            var productCategories = (await _productService.GetProductById(request.CurrentProductId)).ProductCategories;
            if (productCategories.Any())
                activeCategoryId = productCategories.MinBy(x => x.DisplayOrder).CategoryId;
        }

        var cachedModel = await _mediator.Send(new GetCategorySimple {
            Customer = request.Customer,
            Language = request.Language,
            Store = request.Store,
            CurrentCategoryId = request.CurrentCategoryId
        }, cancellationToken);

        var model = new CategoryNavigationModel {
            CurrentCategoryId = activeCategoryId,
            Categories = cachedModel.ToList()
        };

        return model;
    }
}