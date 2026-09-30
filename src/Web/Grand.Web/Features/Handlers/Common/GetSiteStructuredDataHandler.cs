using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Common;

public class GetSiteStructuredDataHandler : IRequestHandler<GetSiteStructuredData, SiteStructuredDataModel>
{
    private readonly ICountryService _countryService;
    private readonly OrderSettings _orderSettings;
    private readonly IPictureService _pictureService;
    private readonly StoreInformationSettings _storeInformationSettings;

    public GetSiteStructuredDataHandler(
        ICountryService countryService,
        IPictureService pictureService,
        OrderSettings orderSettings,
        StoreInformationSettings storeInformationSettings)
    {
        _countryService = countryService;
        _pictureService = pictureService;
        _orderSettings = orderSettings;
        _storeInformationSettings = storeInformationSettings;
    }

    public async Task<SiteStructuredDataModel> Handle(GetSiteStructuredData request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Store);

        var model = new SiteStructuredDataModel {
            StoreName = request.Store.Name,
            //social profiles the store has actually filled in - sameAs is what links the
            //site to an existing knowledge-graph entity
            SameAs = new[] {
                    _storeInformationSettings.FacebookLink,
                    _storeInformationSettings.TwitterLink,
                    _storeInformationSettings.YoutubeLink,
                    _storeInformationSettings.InstagramLink,
                    _storeInformationSettings.LinkedInLink,
                    _storeInformationSettings.PinterestLink
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()
        };

        if (!string.IsNullOrEmpty(_storeInformationSettings.LogoPictureId))
            model.LogoUrl = await _pictureService.GetPictureUrl(_storeInformationSettings.LogoPictureId);

        if (_orderSettings.MerchandiseReturnsEnabled)
            model.ReturnPolicy = new SiteStructuredDataModel.ReturnPolicyModel {
                //IsMerchandiseReturnAllowedQueryHandler applies no age limit when this is 0
                UnlimitedWindow = _orderSettings.NumberOfDaysMerchandiseReturnAvailable <= 0,
                //optional declarations from the settings screen; NotSpecified publishes nothing
                ReturnFees = _orderSettings.MerchandiseReturnFees switch {
                    MerchandiseReturnFees.Free => "https://schema.org/FreeReturn",
                    MerchandiseReturnFees.CustomerPays => "https://schema.org/ReturnFeesCustomerResponsibility",
                    _ => null
                },
                ReturnMethod = _orderSettings.MerchandiseReturnMethod switch {
                    MerchandiseReturnMethod.ByMail => "https://schema.org/ReturnByMail",
                    MerchandiseReturnMethod.InStore => "https://schema.org/ReturnInStore",
                    _ => null
                },
                CountryCodes = (await _countryService.GetAllCountriesForShipping(storeId: request.Store.Id))
                    .Select(x => x.TwoLetterIsoCode)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList()
            };

        return model;
    }
}
