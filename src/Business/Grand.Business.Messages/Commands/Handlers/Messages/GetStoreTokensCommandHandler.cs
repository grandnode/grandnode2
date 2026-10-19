using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Handlers.Messages;

public class GetStoreTokensCommandHandler : IRequestHandler<GetStoreTokensCommand, LiquidStore>
{
    private readonly StoreInformationSettings _storeInformationSettings;

    public GetStoreTokensCommandHandler(StoreInformationSettings storeInformationSettings)
    {
        _storeInformationSettings = storeInformationSettings;
    }

    public async Task<LiquidStore> Handle(GetStoreTokensCommand request, CancellationToken cancellationToken)
    {
        var liquidStore = new LiquidStore(request.Store, request.Language, request.EmailAccount) {
            TwitterLink = _storeInformationSettings.TwitterLink,
            FacebookLink = _storeInformationSettings.FacebookLink,
            YoutubeLink = _storeInformationSettings.YoutubeLink,
            InstagramLink = _storeInformationSettings.InstagramLink,
            LinkedInLink = _storeInformationSettings.LinkedInLink,
            PinterestLink = _storeInformationSettings.PinterestLink
        };
        return await Task.FromResult(liquidStore);
    }
}