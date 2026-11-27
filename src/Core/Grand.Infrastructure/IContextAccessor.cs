namespace Grand.Infrastructure;

public interface IContextAccessor
{
    /// <summary>
    ///     The work context of the current request; throws when none is set, as outside a request
    /// </summary>
    IWorkContext WorkContext { get; set; }

    /// <summary>
    ///     The store context of the current request; throws when none is set, as outside a request
    /// </summary>
    IStoreContext StoreContext { get; set; }

    /// <summary>
    ///     The work context, or null when none is set: for code that also runs outside a request
    /// </summary>
    IWorkContext? CurrentWorkContext { get; }

    /// <summary>
    ///     The store context, or null when none is set: for code that also runs outside a request
    /// </summary>
    IStoreContext? CurrentStoreContext { get; }
}