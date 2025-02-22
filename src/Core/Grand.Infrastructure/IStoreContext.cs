using Grand.Domain.Stores;

namespace Grand.Infrastructure;

public interface IStoreContext
{
    /// <summary>
    ///     Gets or sets the current store
    /// </summary>
    Store CurrentStore { get; }

    /// <summary>
    ///     Gets the current host
    /// </summary>
    DomainHost CurrentHost { get; }
}

public interface IStoreContextSetter
{
    /// <summary>
    ///    Initialize the store context
    /// </summary>
    /// <returns></returns>
    Task<IStoreContext> InitializeStoreContext(string storeId = null);
}