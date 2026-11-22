namespace Grand.Web.Common.Models;

/// <summary>
///     How far the current customer may enter the storefront; the guest account screens pick their layout from it
/// </summary>
public enum StoreAccess
{
    Open,
    RegisteredOnly,
    Closed
}

/// <summary>
///     A view model that carries the <see cref="StoreAccess" /> of the current customer. The value is set by
///     <see cref="Filters.StoreAccessAttribute" />, never bound from a request
/// </summary>
public interface IStoreAccessModel
{
    StoreAccess StoreAccess { get; set; }
}
