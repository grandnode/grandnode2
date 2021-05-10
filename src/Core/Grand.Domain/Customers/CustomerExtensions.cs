using Grand.Domain.Common;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Domain.Customers
{
    public static class CustomerExtensions
    {
        #region Customer
        /// <summary>
        /// Gets a value indicating whether customer a anonymous
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public static bool IsSystemAccount(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            return customer.IsSystemAccount;
        }

        /// <summary>
        /// Gets a value indicating whether customer a anonymous
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public static bool IsAnonymousAccount(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (!customer.IsSystemAccount || string.IsNullOrEmpty(customer.SystemName))
                return false;

            var result = customer.SystemName.Equals(SystemCustomerNames.Anonymous, StringComparison.OrdinalIgnoreCase);
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customer a search engine
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public static bool IsSearchEngineAccount(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (!customer.IsSystemAccount || string.IsNullOrEmpty(customer.SystemName))
                return false;

            var result = customer.SystemName.Equals(SystemCustomerNames.SearchEngine, StringComparison.OrdinalIgnoreCase);
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether the customer is a built-in for background tasks
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        public static bool IsBackgroundTaskAccount(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (!customer.IsSystemAccount || String.IsNullOrEmpty(customer.SystemName))
                return false;

            var result = customer.SystemName.Equals(SystemCustomerNames.BackgroundTask, StringComparison.OrdinalIgnoreCase);
            return result;
        }

        #endregion

        #region Addresses

        public static void RemoveAddress(this Customer customer, Address address)
        {
            if (customer.Addresses.Contains(address))
            {
                if (customer.BillingAddress == address) customer.BillingAddress = null;
                if (customer.ShippingAddress == address) customer.ShippingAddress = null;

                customer.Addresses.Remove(address);
            }
        }

        #endregion

        #region Customer

        public static string CouponSeparator => ";";

        /// <summary>
        /// Get full name
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Customer full name</returns>
        public static string GetFullName(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));
            var firstName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);
            var lastName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);

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
        /// Formats the customer name
        /// </summary>
        /// <param name="customer">Source</param>
        /// <returns>Formatted text</returns>
        public static string FormatUserName(this Customer customer, CustomerNameFormat customerNameFormat)
        {
            if (customer == null)
                return string.Empty;

            if (string.IsNullOrEmpty(customer.Email))
            {
                return "Customer.Guest";
            }

            string result = string.Empty;
            switch (customerNameFormat)
            {
                case CustomerNameFormat.Emails:
                    result = customer.Email;
                    break;
                case CustomerNameFormat.Usernames:
                    result = customer.Username;
                    break;
                case CustomerNameFormat.FullNames:
                    result = customer.GetFullName();
                    break;
                case CustomerNameFormat.FirstName:
                    result = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets coupon codes
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Coupon codes</returns>
        public static string[] ParseAppliedCouponCodes(this Customer customer, string key)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);

            var couponCodes = new List<string>();
            if (string.IsNullOrEmpty(existingCouponCodes))
                return couponCodes.ToArray();

            return existingCouponCodes.Split(CouponSeparator);

        }

        /// <summary>
        /// Adds a coupon code
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code</param>
        /// <returns>New coupon codes document</returns>
        public static string ApplyCouponCode(this Customer customer, string key, string couponCode)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);
            if (string.IsNullOrEmpty(existingCouponCodes))
            {
                return couponCode;
            }
            else
            {
                return string.Join(CouponSeparator, existingCouponCodes.Split(CouponSeparator).Append(couponCode).Distinct());
            }
        }
        /// <summary>
        /// Adds a coupon codes
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code</param>
        /// <returns>New coupon codes document</returns>
        public static string ApplyCouponCode(this Customer customer, string key, string[] couponCodes)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);
            if (string.IsNullOrEmpty(existingCouponCodes))
            {
                return string.Join(CouponSeparator, couponCodes);
            }
            else
            {
                var coupons = existingCouponCodes.Split(CouponSeparator).ToList();
                coupons.AddRange(couponCodes.ToList());
                return string.Join(CouponSeparator, coupons.Distinct());
            }
        }
        /// <summary>
        /// Adds a coupon code
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="couponCode">Coupon code</param>
        /// <returns>New coupon codes document</returns>
        public static string RemoveCouponCode(this Customer customer, string key, string couponCode)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);
            if (string.IsNullOrEmpty(existingCouponCodes))
            {
                return "";
            }
            else
            {
                return string.Join(CouponSeparator, existingCouponCodes.Split(CouponSeparator).Except(new List<string> { couponCode }).Distinct());
            }
        }

        /// <summary>
        /// Check whether password recovery token is valid
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="token">Token to validate</param>
        /// <returns>Result</returns>
        public static bool IsPasswordRecoveryTokenValid(this Customer customer, string token)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var cPrt = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryToken);
            if (String.IsNullOrEmpty(cPrt))
                return false;

            if (!cPrt.Equals(token, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Check whether password recovery link is expired
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="customerSettings">Customer settings</param>
        /// <returns>Result</returns>
        public static bool IsPasswordRecoveryLinkExpired(this Customer customer, CustomerSettings customerSettings)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (customerSettings == null)
                throw new ArgumentNullException(nameof(customerSettings));

            if (customerSettings.PasswordRecoveryLinkDaysValid == 0)
                return false;

            var geneatedDate = customer.GetUserFieldFromEntity<DateTime?>(SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated);
            if (!geneatedDate.HasValue)
                return false;

            var daysPassed = (DateTime.UtcNow - geneatedDate.Value).TotalDays;
            if (daysPassed > customerSettings.PasswordRecoveryLinkDaysValid)
                return true;

            return false;
        }

        /// <summary>
        /// Get customer group identifiers
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Customer group identifiers</returns>
        public static string[] GetCustomerGroupIds(this Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            return customer.Groups.ToArray();
        }

        #endregion
    }
}
