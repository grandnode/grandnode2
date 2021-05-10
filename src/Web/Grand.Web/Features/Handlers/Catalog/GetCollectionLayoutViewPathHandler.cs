using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCollectionLayoutViewPathHandler : IRequestHandler<GetCollectionLayoutViewPath, string>
    {
        private readonly ICollectionLayoutService _collectionLayoutService;

        public GetCollectionLayoutViewPathHandler(
            ICollectionLayoutService collectionLayoutService)
        {
            _collectionLayoutService = collectionLayoutService;
        }

        public async Task<string> Handle(GetCollectionLayoutViewPath request, CancellationToken cancellationToken)
        {
            var layout = await _collectionLayoutService.GetCollectionLayoutById(request.LayoutId);
            if (layout == null)
                layout = (await _collectionLayoutService.GetAllCollectionLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;
        }
    }
}
