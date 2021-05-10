using Grand.Business.Common.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Affiliates;
using Grand.Domain.Seo;
using Grand.SharedKernel.Extensions;
using System;
using System.Threading.Tasks;
using Grand.Infrastructure.Extensions;

namespace Grand.Business.Customers.Extensions
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

            string fullName = "";
            if (!String.IsNullOrWhiteSpace(firstName) && !String.IsNullOrWhiteSpace(lastName))
                fullName = string.Format("{0} {1}", firstName, lastName);
            else
            {
                if (!String.IsNullOrWhiteSpace(firstName))
                    fullName = firstName;

                if (!String.IsNullOrWhiteSpace(lastName))
                    fullName = lastName;
            }
            return fullName;
        }


        /// <summary>
        /// Generate affilaite URL
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="workContext">WorkContext</param>
        /// <returns>Generated affilaite URL</returns>
        public static string GenerateUrl(this Affiliate affiliate, IWorkContext workContext)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            if (workContext == null)
                throw new ArgumentNullException(nameof(workContext));

            var storeUrl = workContext.CurrentStore.Url.TrimEnd('/');
            var url = !String.IsNullOrEmpty(affiliate.FriendlyUrlName) ? 
                CommonExtensions.ModifyQueryString(storeUrl, "affiliate", affiliate.FriendlyUrlName) : CommonExtensions.ModifyQueryString(storeUrl, "affiliateid", affiliate.Id);

            return url;
        }

        /// <summary>
        /// Validate friendly URL name
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="friendlyUrlName">Friendly URL name</param>
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

            if (String.IsNullOrEmpty(friendlyUrlName))
                return friendlyUrlName;
            //check whether such friendly URL name already exists (and that is not the current affiliate)
            int i = 2;
            var tempName = friendlyUrlName;
            while (true)
            {
                var affiliateByFriendlyUrlName = await affiliateService.GetAffiliateByFriendlyUrlName(tempName);
                bool reserved = affiliateByFriendlyUrlName != null && affiliateByFriendlyUrlName.Id != affiliate.Id;
                if (!reserved)
                    break;

                tempName = string.Format("{0}-{1}", friendlyUrlName, i);
                i++;
            }
            friendlyUrlName = tempName;

            return friendlyUrlName;
        }
    }
}
