using Grand.Domain.Common;

namespace Grand.Domain.Customers;

public static class CustomerExtensions
{
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

    /// <summary>
    ///     Gets a value indicating whether customer a anonymous
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <returns>Result</returns>
    public static bool IsSystemAccount(this Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return customer.IsSystemAccount && !string.IsNullOrEmpty(customer.SystemName);
    }


    /// <summary>
    ///     Gets a value indicating whether customer a search engine
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <returns>Result</returns>
    public static bool IsSearchEngineAccount(this Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (!customer.IsSystemAccount || string.IsNullOrEmpty(customer.SystemName))
            return false;

        var result = customer.SystemName.Equals(SystemCustomerNames.SearchEngine, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    #endregion

    #region Customer

    public static string CouponSeparator => ";";

    /// <summary>
    ///     Get full name
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <returns>Customer full name</returns>
    public static string GetFullName(this Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var firstName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);
        var lastName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);

        var fullName = "";
        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
        {
            fullName = $"{firstName} {lastName}";
        }
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
    ///     Formats the customer name
    /// </summary>
    /// <param name="customer">Source</param>
    /// <param name="customerNameFormat"></param>
    /// <returns>Formatted text</returns>
    public static string FormatUserName(this Customer customer, CustomerNameFormat customerNameFormat)
    {
        if (customer == null)
            return string.Empty;

        if (string.IsNullOrEmpty(customer.Email)) return "Customer.Guest";

        var result = string.Empty;
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
        }

        return result;
    }

    /// <summary>
    ///     Gets coupon codes
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="key">key</param>
    /// <returns>Coupon codes</returns>
    public static string[] ParseAppliedCouponCodes(this Customer customer, string key)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);

        var couponCodes = new List<string>();
        if (string.IsNullOrEmpty(existingCouponCodes))
            return couponCodes.ToArray();

        return existingCouponCodes.Split(CouponSeparator);
    }

    /// <summary>
    ///     Adds a coupon code
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="key"></param>
    /// <param name="couponCode">Coupon code</param>
    /// <returns>New coupon codes document</returns>
    public static string ApplyCouponCode(this Customer customer, string key, string couponCode)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);
        return string.IsNullOrEmpty(existingCouponCodes)
            ? couponCode
            : string.Join(CouponSeparator, existingCouponCodes.Split(CouponSeparator).Append(couponCode).Distinct());
    }

    /// <summary>
    ///     Adds a coupon codes
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="key">key</param>
    /// <param name="couponCodes">Coupon code</param>
    /// <returns>New coupon codes document</returns>
    public static string ApplyCouponCode(this Customer customer, string key, string[] couponCodes)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);
        if (string.IsNullOrEmpty(existingCouponCodes)) return string.Join(CouponSeparator, couponCodes);

        var coupons = existingCouponCodes.Split(CouponSeparator).ToList();
        coupons.AddRange(couponCodes.ToList());
        return string.Join(CouponSeparator, coupons.Distinct());
    }

    /// <summary>
    ///     Adds a coupon code
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="key"></param>
    /// <param name="couponCode">Coupon code</param>
    /// <returns>New coupon codes document</returns>
    public static string RemoveCouponCode(this Customer customer, string key, string couponCode)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var existingCouponCodes = customer.GetUserFieldFromEntity<string>(key);
        return string.IsNullOrEmpty(existingCouponCodes)
            ? ""
            : string.Join(CouponSeparator,
                existingCouponCodes.Split(CouponSeparator).Except(new List<string> { couponCode }).Distinct());
    }

    /// <summary>
    ///     Check whether password recovery token is valid
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="token">Token to validate</param>
    /// <returns>Result</returns>
    public static bool IsPasswordRecoveryTokenValid(this Customer customer, string token)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var cPrt = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryToken);
        return !string.IsNullOrEmpty(cPrt) && cPrt.Equals(token, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Check whether password recovery link is expired
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="customerSettings">Customer settings</param>
    /// <returns>Result</returns>
    public static bool IsPasswordRecoveryLinkExpired(this Customer customer, CustomerSettings customerSettings)
    {
        ArgumentNullException.ThrowIfNull(customer);

        ArgumentNullException.ThrowIfNull(customerSettings);

        if (customerSettings.PasswordRecoveryLinkDaysValid == 0)
            return false;

        var geneatedDate =
            customer.GetUserFieldFromEntity<DateTime?>(SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated);
        if (!geneatedDate.HasValue)
            return false;

        var daysPassed = (DateTime.UtcNow - geneatedDate.Value).TotalDays;
        return daysPassed > customerSettings.PasswordRecoveryLinkDaysValid;
    }

    /// <summary>
    ///     Get customer group identifiers
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <returns>Customer group identifiers</returns>
    public static string[] GetCustomerGroupIds(this Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return customer.Groups.ToArray();
    }

    #endregion
}