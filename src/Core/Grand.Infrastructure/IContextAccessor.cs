namespace Grand.Infrastructure;

public interface IContextAccessor
{
    IWorkContext WorkContext { get; set; }

    IStoreContext StoreContext { get; set; }
}
