using Grand.Infrastructure;

namespace Grand.Web.Common;

public class ContextAccessor : IContextAccessor
{
    private static readonly AsyncLocal<IWorkContext?> _asyncLocalWorkContext = new();
    private static readonly AsyncLocal<IStoreContext?> _asyncLocalStoreContext = new();

    public IWorkContext WorkContext {
        get => _asyncLocalWorkContext.Value ??
               throw new InvalidOperationException("No work context is set: ContextMiddleware sets it for a request");
        set => _asyncLocalWorkContext.Value = value;
    }

    public IStoreContext StoreContext {
        get => _asyncLocalStoreContext.Value ??
               throw new InvalidOperationException("No store context is set: ContextMiddleware sets it for a request");
        set => _asyncLocalStoreContext.Value = value;
    }

    public IWorkContext? CurrentWorkContext => _asyncLocalWorkContext.Value;

    public IStoreContext? CurrentStoreContext => _asyncLocalStoreContext.Value;
}