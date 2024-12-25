namespace Grand.Infrastructure;

public interface IWorkContextAccessor
{
    IWorkContext WorkContext { get; set; }
}
