using Grand.Business.Common.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetVendorNavigationHandler : IRequestHandler<GetVendorNavigation, VendorNavigationModel>
    {
        private readonly ICacheBase _cacheBase;
        private readonly IVendorService _vendorService;
        private readonly VendorSettings _vendorSettings;

        public GetVendorNavigationHandler(ICacheBase cacheBase, IVendorService vendorService, VendorSettings vendorSettings)
        {
            _cacheBase = cacheBase;
            _vendorService = vendorService;
            _vendorSettings = vendorSettings;
        }

        public async Task<VendorNavigationModel> Handle(GetVendorNavigation request, CancellationToken cancellationToken)
        {
            string cacheKey = CacheKeyConst.VENDOR_NAVIGATION_MODEL_KEY;
            var cacheModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var vendors = await _vendorService.GetAllVendors(pageSize: _vendorSettings.VendorsBlockItemsToDisplay);
                var model = new VendorNavigationModel
                {
                    TotalVendors = vendors.TotalCount
                };

                foreach (var vendor in vendors)
                {
                    model.Vendors.Add(new VendorBriefInfoModel
                    {
                        Id = vendor.Id,
                        Name = vendor.GetTranslation(x => x.Name, request.Language.Id),
                        SeName = vendor.GetSeName(request.Language.Id),
                    });
                }
                return model;
            });
            return cacheModel;
        }
    }
}
