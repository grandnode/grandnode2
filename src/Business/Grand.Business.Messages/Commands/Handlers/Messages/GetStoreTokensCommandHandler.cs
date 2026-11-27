using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Handlers.Messages;

public class GetStoreTokensCommandHandler : IRequestHandler<GetStoreTokensCommand, LiquidStore>
{
    private readonly ISettingService _settingService;

    public GetStoreTokensCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<LiquidStore> Handle(GetStoreTokensCommand request, CancellationToken cancellationToken)
    {
        //the message's store, not the current one: a scheduled task or another store's admin sends it too
        var storeInformationSettings =
            await _settingService.LoadSetting<StoreInformationSettings>(request.Store.Id);
        return new LiquidStore(request.Store, request.Language, request.EmailAccount) {
            TwitterLink = storeInformationSettings.TwitterLink,
            FacebookLink = storeInformationSettings.FacebookLink,
            YoutubeLink = storeInformationSettings.YoutubeLink,
            InstagramLink = storeInformationSettings.InstagramLink,
            LinkedInLink = storeInformationSettings.LinkedInLink,
            PinterestLink = storeInformationSettings.PinterestLink
        };
    }
}