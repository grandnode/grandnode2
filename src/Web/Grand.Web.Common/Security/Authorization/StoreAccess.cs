namespace Grand.Web.Common.Security.Authorization;

/// <summary>
///     How far the customer of the current request may enter the storefront
/// </summary>
public enum StoreAccess
{
    Open,
    RegisteredOnly,
    Closed
}
