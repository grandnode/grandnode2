using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Represents a task end auctions
/// </summary>
public class EndAuctionsTask : IScheduleTask
{
    private readonly IAuctionService _auctionService;
    private readonly ICustomerService _customerService;
    private readonly ILanguageService _languageService;
    private readonly LanguageSettings _languageSettings;
    private readonly ILogger<EndAuctionsTask> _logger;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IShoppingCartService _shoppingCartService;

    public EndAuctionsTask(IAuctionService auctionService,
        IMessageProviderService messageProviderService, LanguageSettings translationService,
        IShoppingCartService shoppingCartService,
        ICustomerService customerService, ILanguageService languageService, ILogger<EndAuctionsTask> logger)
    {
        _languageService = languageService;
        _auctionService = auctionService;
        _messageProviderService = messageProviderService;
        _languageSettings = translationService;
        _shoppingCartService = shoppingCartService;
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    ///     Executes a task
    /// </summary>
    public async Task Execute(CancellationToken cancellationToken)
    {
        var auctionsToEnd = await _auctionService.GetAuctionsToEnd();
        if (!auctionsToEnd.Any()) return;

        //cart warnings only reach the log, so they are written in the admin language
        var language =
            await _languageService.GetLanguageOrStoreDefault(_languageSettings.DefaultAdminLanguageId, null);
        foreach (var auctionToEnd in auctionsToEnd)
        {
            //between auctions only - ending one must finish once started
            cancellationToken.ThrowIfCancellationRequested();

            var bid = (await _auctionService.GetBidsByProductId(auctionToEnd.Id)).MaxBy(x => x.Amount);
            if (bid == null)
            {
                await _auctionService.UpdateAuctionEnded(auctionToEnd, true);
                await _messageProviderService.SendAuctionEndedStoreOwnerMessage(auctionToEnd,
                    _languageSettings.DefaultAdminLanguageId, null);
                continue;
            }

            if (language == null)
            {
                _logger.LogWarning("EndAuctionTask - Product {Name} - no language exists to validate the bid",
                    auctionToEnd.Name);
                continue;
            }

            var warnings = (await _shoppingCartService.AddToCart(await _customerService.GetCustomerById(bid.CustomerId),
                language,
                bid.ProductId, ShoppingCartType.Auctions,
                bid.StoreId, bid.WarehouseId, customerEnteredPrice: bid.Amount,
                validator: new ShoppingCartValidatorOptions { GetRequiredProductWarnings = false })).warnings;

            if (!warnings.Any())
            {
                bid.Win = true;
                await _auctionService.UpdateBid(bid);
                await _messageProviderService.SendAuctionEndedStoreOwnerMessage(auctionToEnd,
                    _languageSettings.DefaultAdminLanguageId, bid);
                await _messageProviderService.SendAuctionWinEndedCustomerMessage(auctionToEnd, null, bid);
                await _messageProviderService.SendAuctionEndedLostCustomerMessage(auctionToEnd, null, bid);
                await _auctionService.UpdateAuctionEnded(auctionToEnd, true);
            }
            else
            {
                _logger.LogInformation("EndAuctionTask - Product {Name} - warnings {Join}", auctionToEnd.Name,
                    string.Join(",", warnings.ToArray()));
            }
        }
    }
}