using Grand.Infrastructure;

namespace Grand.Web.Common;

public class WorkContextAccessor : IWorkContextAccessor
{
    private static readonly AsyncLocal<IWorkContext> _asyncLocalWorkContext = new();

    public IWorkContext WorkContext {
        get => _asyncLocalWorkContext.Value;
        set => _asyncLocalWorkContext.Value = value;
    }
}
