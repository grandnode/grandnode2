﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

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
            var cacheModel = await _cacheBase.GetAsync(CacheKeyConst.VENDOR_NAVIGATION_MODEL_KEY, async () =>
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
                        SeName = vendor.GetSeName(request.Language.Id)
                    });
                }
                return model;
            });
            return cacheModel;
        }
    }
}
