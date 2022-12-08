﻿using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

namespace Grand.Business.Messages.Services
{
    public class MessageTokenProvider : IMessageTokenProvider
    {
        #region Methods

        /// <summary>
        /// Gets list of allowed (supported) message tokens for campaigns
        /// </summary>
        /// <returns>List of allowed (supported) message tokens for campaigns</returns>
        public virtual string[] GetListOfCampaignAllowedTokens()
        {
            var allowedTokens = LiquidExtensions.GetTokens(typeof(LiquidStore),
                typeof(LiquidNewsLetterSubscription),
                typeof(LiquidShoppingCart),
                typeof(LiquidCustomer));

            return allowedTokens.ToArray();
        }

        public virtual string[] GetListOfAllowedTokens()
        {
            var allowedTokens = LiquidExtensions.GetTokens(
                typeof(LiquidAskQuestion),
                typeof(LiquidAttributeCombination),
                typeof(LiquidAuctions),
                typeof(LiquidOutOfStockSubscription),
                typeof(LiquidBlogComment),
                typeof(LiquidContactUs),
                typeof(LiquidCustomer),
                typeof(LiquidEmailAFriend),
                typeof(LiquidGiftVoucher),
                typeof(LiquidKnowledgebase),
                typeof(LiquidNewsComment),
                typeof(LiquidNewsLetterSubscription),
                typeof(LiquidOrder),
                typeof(LiquidOrderItem),
                typeof(LiquidProduct),
                typeof(LiquidProductReview),
                typeof(LiquidMerchandiseReturn),
                typeof(LiquidShipment),
                typeof(LiquidShipmentItem),
                typeof(LiquidShoppingCart),
                typeof(LiquidStore),
                typeof(LiquidVatValidationResult),
                typeof(LiquidVendor),
                typeof(LiquidVendorReview));

            return allowedTokens.ToArray();
        }

        #endregion
    }
}