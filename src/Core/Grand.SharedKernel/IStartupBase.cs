namespace Grand.SharedKernel;

public interface IStartupBase
{
    /// <summary>
    ///     Priority
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Execute
    /// </summary>
    void Execute();
}