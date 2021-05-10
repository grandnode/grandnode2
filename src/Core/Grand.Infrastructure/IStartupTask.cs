namespace Grand.Infrastructure
{
    /// <summary>
    /// Interface which should be implemented by void run on startup
    /// </summary>
    public interface IStartupTask
    {
        /// <summary>
        /// Executes during startup application
        /// </summary>
        void Execute();

        /// <summary>
        /// Order
        /// </summary>
        int Order { get; }
    }
}
