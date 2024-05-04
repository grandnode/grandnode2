using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
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
    ///     Gets the current host
    /// </summary>
    DomainHost CurrentHost { get; }

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

    /// <summary>
    ///     Gets or sets the current store
    /// </summary>
    Store CurrentStore { get; }
}

public interface IWorkContextSetter
{
    /// <summary>
    ///     Set the current customer by Middleware
    /// </summary>
    /// <returns></returns>
    Task<Customer> SetCurrentCustomer();

    /// <summary>
    ///     Set the current customer
    /// </summary>
    /// <returns></returns>
    Task<Customer> SetCurrentCustomer(Customer customer);

    /// <summary>
    ///     Set the current vendor (logged-in manager)
    /// </summary>
    Task<Vendor> SetCurrentVendor(Customer customer);

    /// <summary>
    ///     Set current user working language by Middleware
    /// </summary>
    Task<Language> SetWorkingLanguage(Customer customer);

    /// <summary>
    ///     Set current user working currency by Middleware
    /// </summary>
    Task<Currency> SetWorkingCurrency(Customer customer);

    /// <summary>
    ///     Set current tax display type by Middleware
    /// </summary>
    Task<TaxDisplayType> SetTaxDisplayType(Customer customer);
}