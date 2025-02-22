using Grand.Infrastructure;

namespace Grand.Web.Common;

public class ContextAccessor : IContextAccessor
{
    private static readonly AsyncLocal<IWorkContext> _asyncLocalWorkContext = new();
    private static readonly AsyncLocal<IStoreContext> _asyncLocalStoreContext = new();

    public IWorkContext WorkContext {
        get => _asyncLocalWorkContext.Value;
        set => _asyncLocalWorkContext.Value = value;
    }
    public IStoreContext StoreContext {
        get => _asyncLocalStoreContext.Value;
        set => _asyncLocalStoreContext.Value = value;
    }
}
