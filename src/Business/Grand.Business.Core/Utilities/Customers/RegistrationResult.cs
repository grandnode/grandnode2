namespace Grand.Business.Core.Utilities.Customers
{
    /// <summary>
    /// Customer Registration result
    /// </summary>
    public class RegistrationResult 
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public RegistrationResult() 
        {
            this.Errors = new List<string>();
        }

        /// <summary>
        /// Gets a value indicating whether request has been completed successfully
        /// </summary>
        public bool Success => this.Errors.Count == 0;

        /// <summary>
        /// Add error
        /// </summary>
        /// <param name="error">Error</param>
        public void AddError(string error) 
        {
            this.Errors.Add(error);
        }

        /// <summary>
        /// Errors
        /// </summary>
        public IList<string> Errors { get; set; }
    }
}
