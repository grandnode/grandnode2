using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;

namespace Grand.Infrastructure;

/// <summary>
///     Work context - get current information about current customer/store
/// </summary>
public interface IWorkContext
{
    /// <summary>
    ///     Gets the current customer
    /// </summary>
    Customer CurrentCustomer { get; }

    /// <summary>
    ///     Gets or sets the original customer (in case the current one is impersonated)
    /// </summary>
    Customer OriginalCustomerIfImpersonated { get; }

    /// <summary>
    ///     Gets the current vendor (logged-in manager)
    /// </summary>
    Vendor CurrentVendor { get; }

    /// <summary>
    ///     Get or set current user working language
    /// </summary>
    Language WorkingLanguage { get; }

    /// <summary>
    ///     Get or set current user working currency
    /// </summary>
    Currency WorkingCurrency { get; }

    /// <summary>
    ///     Get current tax display type
    /// </summary>
    TaxDisplayType TaxDisplayType { get; }
}

public interface IWorkContextSetter
{
    /// <summary>
    ///    Initialize the work context
    /// </summary>
    /// <returns></returns>
    Task<IWorkContext> InitializeWorkContext(string storeId = null);
}