﻿using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Affiliates;
using Grand.Domain.Seo;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Core.Extensions
{
    public static class AffiliateExtensions
    {
        /// <summary>
        /// Get full name
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <returns>Affiliate full name</returns>
        public static string GetFullName(this Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            var firstName = affiliate.Address.FirstName;
            var lastName = affiliate.Address.LastName;

            var fullName = "";
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                fullName = $"{firstName} {lastName}";
            else
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                    fullName = firstName;

                if (!string.IsNullOrWhiteSpace(lastName))
                    fullName = lastName;
            }
            return fullName;
        }


        /// <summary>
        /// Generate affiliate URL
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="host">Host</param>
        /// <returns>Generated affiliate URL</returns>
        public static string GenerateUrl(this Affiliate affiliate, string host)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException(nameof(host));

            var url = !string.IsNullOrEmpty(affiliate.FriendlyUrlName) ?
                CommonExtensions.ModifyQueryString(host, "affiliate", affiliate.FriendlyUrlName)
                : CommonExtensions.ModifyQueryString(host, "affiliateid", affiliate.Id);

            return url;
        }

        /// <summary>
        /// Validate friendly URL name
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="seoSettings"></param>
        /// <param name="friendlyUrlName">Friendly URL name</param>
        /// <param name="affiliateService"></param>
        /// <param name="name"></param>
        /// <returns>Valid friendly name</returns>
        public static async Task<string> ValidateFriendlyUrlName(this Affiliate affiliate, IAffiliateService affiliateService, SeoSettings seoSettings, string friendlyUrlName, string name)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            if (string.IsNullOrEmpty(friendlyUrlName))
                friendlyUrlName = name;

            //ensure we have only valid chars
            friendlyUrlName = SeoExtensions.GetSeName(friendlyUrlName, seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);

            //max length
            friendlyUrlName = CommonHelper.EnsureMaximumLength(friendlyUrlName, 200);

            if (string.IsNullOrEmpty(friendlyUrlName))
                return friendlyUrlName;
            //check whether such friendly URL name already exists (and that is not the current affiliate)
            var i = 2;
            var tempName = friendlyUrlName;
            while (true)
            {
                var affiliateByFriendlyUrlName = await affiliateService.GetAffiliateByFriendlyUrlName(tempName);
                var reserved = affiliateByFriendlyUrlName != null && affiliateByFriendlyUrlName.Id != affiliate.Id;
                if (!reserved)
                    break;

                tempName = $"{friendlyUrlName}-{i}";
                i++;
            }
            friendlyUrlName = tempName;

            return friendlyUrlName;
        }
    }
}
