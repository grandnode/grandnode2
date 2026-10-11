using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;

namespace Grand.Web.Common.Security.ExternalRegistration;

public interface IExternalRegistrationContext
{
    /// <summary>The pending identity of this browser, or null</summary>
    Task<ExternalRegistrationIdentity> GetPending();

    Task Clear();

    /// <summary>Connects the pending identity to the customer and clears it (also when connecting fails)</summary>
    /// <param name="customer">Customer to connect</param>
    /// <param name="storeId">Current store</param>
    Task<OidcLinkResult> Complete(Customer customer, string storeId);
}
