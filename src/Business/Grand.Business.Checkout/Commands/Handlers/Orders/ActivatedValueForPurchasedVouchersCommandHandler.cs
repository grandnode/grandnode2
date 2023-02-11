﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ActivatedValueForPurchasedGiftVouchersCommandHandler : IRequestHandler<ActivatedValueForPurchasedGiftVouchersCommand, bool>
    {
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly ILanguageService _languageService;
        private readonly IMessageProviderService _messageProviderService;

        public ActivatedValueForPurchasedGiftVouchersCommandHandler(
            IGiftVoucherService giftVoucherService,
            ILanguageService languageService,
            IMessageProviderService messageProviderService)
        {
            _giftVoucherService = giftVoucherService;
            _languageService = languageService;
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(ActivatedValueForPurchasedGiftVouchersCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            foreach (var orderItem in request.Order.OrderItems)
            {
                var giftVouchers = await _giftVoucherService.GetAllGiftVouchers(purchasedWithOrderItemId: orderItem.Id,
                    isGiftVoucherActivated: !request.Activate);
                foreach (var gc in giftVouchers)
                {
                    if (request.Activate)
                    {
                        //activate
                        if (gc.GiftVoucherTypeId == GiftVoucherType.Virtual)
                        {
                            //send email for virtual gift voucher
                            if (!string.IsNullOrEmpty(gc.RecipientEmail) &&
                                !string.IsNullOrEmpty(gc.SenderEmail))
                            {
                                var customerLang = await _languageService.GetLanguageById(request.Order.CustomerLanguageId) ??
                                                   (await _languageService.GetAllLanguages()).FirstOrDefault();
                                if (customerLang == null)
                                    throw new Exception("No languages could be loaded");
                                _ = await _messageProviderService.SendGiftVoucherMessage(gc, request.Order, customerLang.Id);
                                gc.IsRecipientNotified = true;
                            }
                        }
                        gc.IsGiftVoucherActivated = true;
                        await _giftVoucherService.UpdateGiftVoucher(gc);
                    }
                    else
                    {
                        //deactivate
                        gc.IsGiftVoucherActivated = false;
                        await _giftVoucherService.UpdateGiftVoucher(gc);
                    }
                }
            }

            return true;
        }
    }
}
