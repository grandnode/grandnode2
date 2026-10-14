using Grand.Domain.Customers;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     The guest of this request that is not saved yet: saved on its first write, or by a later request that
///     returns its cookie
/// </summary>
public interface IGuestCustomerPersistence
{
    /// <summary>
    ///     The unsaved guest of this request; null once saved or when the customer came from the database
    /// </summary>
    Customer Transient { get; }

    void SetTransient(Customer customer);

    bool IsTransient(string customerId);

    /// <summary>
    ///     Saves the transient guest once; later calls do nothing
    /// </summary>
    Task EnsurePersisted();

    /// <summary>
    ///     Saves a guest for a returned cookie whose GUID no customer has. Returns the saved guest, the guest a
    ///     parallel request saved first, or null when the GUID turned out to belong to a customer that is not a guest
    /// </summary>
    Task<Customer> Promote(Customer guest);
}
