namespace Grand.SharedKernel
{
    public interface IStartupBase
    {
        /// <summary>
        /// Execute 
        /// </summary>
        void Execute();

        /// <summary>
        /// Priority
        /// </summary>
        int Priority { get; }
    }
}
